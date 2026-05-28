using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PasswordRecovery;

public class WizarrConnectionTestTask : IScheduledTask
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WizarrConnectionTestTask> _logger;

    public WizarrConnectionTestTask(IHttpClientFactory httpClientFactory, ILogger<WizarrConnectionTestTask> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Password Recovery (for Wizarr) - Test Wizarr Connection";
    public string Key => "PasswordRecoveryTestWizarr";
    public string Description => "Tests Wizarr API connectivity and authentication.";
    public string Category => "Library";
    public bool IsHidden => false;
    public bool IsEnabled => true;
    public bool IsLogged => true;

    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
            if (string.IsNullOrWhiteSpace(config.WizarrBaseUrl) || string.IsNullOrWhiteSpace(config.WizarrApiKey))
            {
                throw new InvalidOperationException("WizarrBaseUrl or WizarrApiKey is missing in plugin configuration.");
            }

            if (!Uri.TryCreate(config.WizarrBaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException("WizarrBaseUrl is invalid. Expected absolute URL, e.g. http://wizarr:5690");
            }

            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Add("X-API-Key", config.WizarrApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"{config.WizarrBaseUrl.TrimEnd('/')}/api/users";
            using var resp = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException("Wizarr authentication failed (401/403). Check X-API-Key.");
                }

                throw new InvalidOperationException($"Wizarr API returned {(int)resp.StatusCode} {resp.ReasonPhrase}.");
            }

            if (LooksLikeHtml(body))
            {
                throw new InvalidOperationException("Wizarr response is HTML instead of JSON. Check WizarrBaseUrl/reverse proxy route.");
            }

            var parsed = JsonSerializer.Deserialize<WizarrUsersResponse>(body);
            if (parsed is null)
            {
                throw new InvalidOperationException("Wizarr returned empty JSON response.");
            }

            _logger.LogInformation("PasswordRecovery Wizarr test OK. users_count={Count}", parsed.count);
            progress.Report(100);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Wizarr request timed out (15s). Server may be down or unreachable.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Unable to reach Wizarr server. Check hostname, port, and reverse proxy.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Wizarr returned invalid JSON. Check API endpoint and proxy configuration.", ex);
        }
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

    private sealed class WizarrUsersResponse
    {
        public int count { get; set; }
    }

    private static bool LooksLikeHtml(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        return body.TrimStart().StartsWith("<", StringComparison.Ordinal);
    }
}

public class SmtpConnectionTestTask : IScheduledTask
{
    private readonly ILogger<SmtpConnectionTestTask> _logger;

    public SmtpConnectionTestTask(ILogger<SmtpConnectionTestTask> logger)
    {
        _logger = logger;
    }

    public string Name => "Password Recovery (for Wizarr) - Test SMTP";
    public string Key => "PasswordRecoveryTestSmtp";
    public string Description => "Sends a test email using configured SMTP settings.";
    public string Category => "Library";
    public bool IsHidden => false;
    public bool IsEnabled => true;
    public bool IsLogged => true;

    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        if (string.IsNullOrWhiteSpace(config.SmtpHost) || string.IsNullOrWhiteSpace(config.FromEmail))
        {
            throw new InvalidOperationException("SMTP host/from are missing in plugin configuration.");
        }

        var to = string.IsNullOrWhiteSpace(config.TestEmailTo) ? config.FromEmail : config.TestEmailTo;
        var subject = "Password Recovery SMTP test";
        var body = "This is a test email from Jellyfin Password Recovery plugin.";

        var fromAddress = string.IsNullOrWhiteSpace(config.FromDisplayName)
            ? new MailAddress(config.FromEmail)
            : new MailAddress(config.FromEmail, config.FromDisplayName);
        using var message = new MailMessage(fromAddress, new MailAddress(to))
        {
            Subject = subject,
            Body = body
        };
        using var smtp = new SmtpClient(config.SmtpHost, config.SmtpPort)
        {
            EnableSsl = config.SmtpUseSsl
        };
        if (!string.IsNullOrWhiteSpace(config.SmtpUsername))
        {
            smtp.Credentials = new NetworkCredential(config.SmtpUsername, config.SmtpPassword);
        }

        smtp.Send(message);
        _logger.LogInformation("PasswordRecovery SMTP test OK. target={Target}", to);
        progress.Report(100);
        return Task.CompletedTask;
    }

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();
}


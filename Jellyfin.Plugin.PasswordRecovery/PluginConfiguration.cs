using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PasswordRecovery;

public class PluginConfiguration : BasePluginConfiguration
{
    public bool Enabled { get; set; } = true;

    public string WizarrBaseUrl { get; set; } = "http://localhost:5690";

    public string WizarrApiKey { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public bool SmtpUseSsl { get; set; } = true;

    public string SmtpUsername { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string TestEmailTo { get; set; } = string.Empty;

    public string EmailSubject { get; set; } = "Reset your Jellyfin password";

    public string EmailBodyTemplate { get; set; } =
        "Hello {username},\n\n" +
        "Use the link below to reset your password:\n" +
        "{reset_link}\n\n" +
        "This link expires in 24 hours.\n";

    public int MinMinutesBetweenEmailsPerUser { get; set; } = 10;
}


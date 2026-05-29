using System;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.PasswordRecovery;

/// <summary>
/// Registers file transformations with the File Transformation plugin (if installed)
/// to patch the forgotPassword page — redirecting to /login instead of the PIN page,
/// and replacing the confusing "PIN file created" message with a friendly email notice.
/// </summary>
public static class FileTransformationRegistrator
{
    private const string ForgotPasswordFilePattern = @"session-forgotPassword\.[a-f0-9]+\.chunk\.js";
    private const string LangFilePattern = @"en-us-json\.[a-f0-9]+\.chunk\.js";

    public static void Register(ILogger logger)
    {
        try
        {
            Assembly? fileTransformationAssembly = null;
            foreach (var ctx in AssemblyLoadContext.All)
            {
                foreach (var asm in ctx.Assemblies)
                {
                    if (asm.FullName?.Contains(".FileTransformation") ?? false)
                    {
                        fileTransformationAssembly = asm;
                        break;
                    }
                }
                if (fileTransformationAssembly != null) break;
            }

            if (fileTransformationAssembly == null)
            {
                logger.LogInformation("PasswordRecovery: File Transformation plugin not found, skipping UI patch registration.");
                return;
            }

            var pluginInterfaceType = fileTransformationAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
            if (pluginInterfaceType == null)
            {
                logger.LogWarning("PasswordRecovery: Could not find PluginInterface type in File Transformation assembly.");
                return;
            }

            var registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
            if (registerMethod == null)
            {
                logger.LogWarning("PasswordRecovery: Could not find RegisterTransformation method.");
                return;
            }

            // Registration 1: patch forgotPassword JS chunk — redirect to /login, remove PIN message
            var forgotPasswordPayload = new JObject
            {
                ["id"] = "a1b2c3d4-0001-0001-0001-000000000001",
                ["fileNamePattern"] = ForgotPasswordFilePattern,
                ["callbackAssembly"] = typeof(FileTransformationRegistrator).Assembly.FullName,
                ["callbackClass"] = typeof(FileTransformationRegistrator).FullName,
                ["callbackMethod"] = nameof(PatchForgotPasswordChunk)
            };

            registerMethod.Invoke(null, new object?[] { forgotPasswordPayload });
            logger.LogInformation("PasswordRecovery: Registered forgotPassword JS transformation.");

            // Registration 2: patch en-us language chunk — replace PIN file message with email notice
            var langPayload = new JObject
            {
                ["id"] = "a1b2c3d4-0002-0002-0002-000000000002",
                ["fileNamePattern"] = LangFilePattern,
                ["callbackAssembly"] = typeof(FileTransformationRegistrator).Assembly.FullName,
                ["callbackClass"] = typeof(FileTransformationRegistrator).FullName,
                ["callbackMethod"] = nameof(PatchLangChunk)
            };

            registerMethod.Invoke(null, new object?[] { langPayload });
            logger.LogInformation("PasswordRecovery: Registered language chunk transformation.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PasswordRecovery: Failed to register File Transformation patches. UI will show default PIN flow.");
        }
    }

    /// <summary>
    /// Called by File Transformation when session-forgotPassword.*.chunk.js is requested.
    /// Redirects to /login instead of /forgotpasswordpin after form submit.
    /// </summary>
    public static string PatchForgotPasswordChunk(JObject contentsObj)
    {
        try
        {
            var contents = contentsObj["contents"]?.ToString() ?? string.Empty;

            contents = contents.Replace(
                "t(\"/forgotpasswordpin\")",
                "t(\"/login\")",
                StringComparison.Ordinal);

            contents = contents.Replace(
                "r+=y.Ay.translate(\"MessageForgotPasswordPinReset\"),r+=\"<br/><br/>\",r+=e.PinFile,r+=\"<br/>\",",
                "r+=\"<br/>\",",
                StringComparison.Ordinal);

            return contents;
        }
        catch
        {
            return contentsObj["contents"]?.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Called by File Transformation when en-us-json.*.chunk.js is requested.
    /// Replaces the confusing PIN file message with a friendly email notice.
    /// </summary>
    public static string PatchLangChunk(JObject contentsObj)
    {
        try
        {
            var contents = contentsObj["contents"]?.ToString() ?? string.Empty;

            contents = contents.Replace(
                "The following file has been created on your server and contains instructions on how to proceed",
                "A password reset email has been sent to your registered email address. Please check your inbox.",
                StringComparison.Ordinal);

            return contents;
        }
        catch
        {
            return contentsObj["contents"]?.ToString() ?? string.Empty;
        }
    }
}

using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PasswordRecovery;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public static Plugin? Instance { get; private set; }

    public override string Name => "Password Recovery (for Wizarr)";

    public override Guid Id => Guid.Parse("d0b38ed3-8017-4e88-9c60-6d6b5e2b7f19");

    public override string Description => "Automatically sends Wizarr password reset links when Jellyfin generates forgot-password PIN files.";

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "passwordrecovery",
                EmbeddedResourcePath = $"{GetType().Namespace}.Web.configPage.html"
            }
        };
    }
}


using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.PasswordRecovery;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient();
        serviceCollection.AddHostedService<PasswordResetFileWatcher>();
        serviceCollection.AddSingleton<IScheduledTask, WizarrConnectionTestTask>();
        serviceCollection.AddSingleton<IScheduledTask, SmtpConnectionTestTask>();
    }
}


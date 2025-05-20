using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Nivot.Aspire.Hosting.PowerShell;

internal class PowerShellScriptLifecycleHook(ResourceNotificationService notificationService, ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{
    public Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var scripts = appModel.Resources.OfType<PowerShellScriptResource>().ToList();
        
        foreach (var scriptResource in scripts)
        {
            var scriptName = scriptResource.Name;
            var scriptLogger = loggerService.GetLogger(scriptName);
            try
            {
                // TODO: capture script streams and log them
                scriptLogger.LogInformation("Starting script '{ScriptName}'", scriptName);

                _ = notificationService
                        .WaitForDependenciesAsync(scriptResource, cancellationToken)
                        .ContinueWith(
                            async (_) => await scriptResource.StartAsync(scriptLogger, notificationService, cancellationToken),
                            cancellationToken);
            }
            catch (Exception ex)
            {
                scriptLogger.LogError(ex, "Failed to start script '{ScriptName}'", scriptName);
            }
        }

        return Task.CompletedTask;
    }
}
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace AspirePowerShell.AppHost;

internal class PowerShellScriptLifecycleHook(ResourceNotificationService notificationService, ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var scripts = appModel.Resources.OfType<PowerShellScriptResource>().ToList();
        var tasks = new List<Task>(scripts.Count);
        foreach (var resource in scripts)
        {
            var scriptName = resource.Name;
            var scriptLogger = loggerService.GetLogger(scriptName);
            try
            {
                // TODO: capture script streams and log them
                scriptLogger.LogInformation("Starting script '{ScriptName}'", scriptName);

                _ =  notificationService
                        .WaitForDependenciesAsync(resource, cancellationToken)
                        .ContinueWith(
                            async (state) => await resource.StartAsync(scriptLogger, notificationService, cancellationToken),
                            cancellationToken);
            }
            catch (Exception ex)
            {
                scriptLogger.LogError(ex, "Failed to start script '{ScriptName}'", scriptName);
            }
        }
    }
}
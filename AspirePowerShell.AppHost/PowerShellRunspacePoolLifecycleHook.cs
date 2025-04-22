using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using System.Management.Automation.Runspaces;

namespace AspirePowerShell.AppHost;

internal class PowerShellRunspacePoolLifecycleHook(ResourceNotificationService notificationService, ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var pools = appModel.Resources.OfType<PowerShellRunspacePoolResource>().ToList();
        var tasks = new List<Task>(pools.Count);

        foreach (var resource in pools) {
            var poolName = resource.Name;
            var poolLogger = loggerService.GetLogger(poolName);

            resource.Pool.StateChanged += async (sender, args) =>
            {
                var poolState = args.RunspacePoolStateInfo.State;
                var reason = args.RunspacePoolStateInfo.Reason;

                poolLogger.LogInformation(
                    "Runspace pool '{PoolName}' state changed to '{RunspacePoolState}'", poolName, poolState);

                // map args.RunspacePoolStateInfo.State to a KnownResourceState
                // and publish the update

                var knownState = poolState switch
                {
                    RunspacePoolState.BeforeOpen => KnownResourceStates.NotStarted,
                    RunspacePoolState.Opening => KnownResourceStates.Starting,
                    RunspacePoolState.Opened => KnownResourceStates.Running,
                    RunspacePoolState.Closing => KnownResourceStates.Stopping,
                    RunspacePoolState.Closed => KnownResourceStates.Exited,
                    RunspacePoolState.Broken => KnownResourceStates.FailedToStart,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(poolState), poolState, "Unexpected runspace pool state")
                };

                await notificationService.PublishUpdateAsync(resource,
                    state => state with
                    {
                        State = knownState,
                        Properties = [.. state.Properties,
                        new("RunspacePoolState", poolState.ToString()),
                        new("Reason", reason?.ToString() ?? string.Empty)
                        ]
                    });
            };

            tasks.Add(Task.Run(() =>
            {
                try
                {
                    poolLogger.LogInformation("Opening runspace pool '{PoolName}'", poolName);
                    resource.Pool.Open();
                }
                catch (Exception ex)
                {
                    poolLogger.LogError(ex, "Failed to open runspace pool '{PoolName}'", poolName);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);

        var scripts = appModel.Resources.OfType<PowerShellScriptResource>().ToList();

        foreach (var script in scripts)
        {
          
            //var scriptBlock = script.ScriptBlock;
            //var runspacePool = scriptResource.Pool;
            //await runspacePool.OpenAsync(cancellationToken);
            //await scriptBlock.InvokeAsync(runspacePool, cancellationToken);
        }

    }

    
}
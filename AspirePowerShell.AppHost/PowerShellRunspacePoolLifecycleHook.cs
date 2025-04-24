using Aspire.Hosting.Lifecycle;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace AspirePowerShell.AppHost;

internal class PowerShellRunspacePoolLifecycleHook(ResourceNotificationService notificationService, ResourceLoggerService loggerService) : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var pools = appModel.Resources.OfType<PowerShellRunspacePoolResource>().ToList();
        var tasks = new List<Task>(pools.Count);

        foreach (var poolResource in pools)
        {
            var sessionState = InitialSessionState.CreateDefault();

            foreach (var annotation in poolResource.Annotations)
            {
                if (annotation is PowerShellVariableReferenceAnnotation<ConnectionStringReference> reference)
                {
                    var connectionString = await reference.Value.Resource.GetConnectionStringAsync(cancellationToken);
                    sessionState.Variables.Add(
                        new SessionStateVariableEntry(reference.Name, connectionString,
                        $"ConnectionString for {reference.Value.Resource.GetType().Name} '{reference.Name}'",
                        ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope));
                }
            }

            var poolName = poolResource.Name;
            var poolLogger = loggerService.GetLogger(poolName);

            _ = notificationService.WaitForDependenciesAsync(poolResource, cancellationToken)
                .ContinueWith(
                    async (state) =>
                        await poolResource.StartAsync(sessionState, notificationService, poolLogger, cancellationToken),
                    cancellationToken);
        }
    }
}
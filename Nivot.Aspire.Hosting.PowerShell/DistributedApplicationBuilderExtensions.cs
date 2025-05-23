﻿using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Nivot.Aspire.Hosting.PowerShell;

public static class DistributedApplicationBuilderExtensions
{
    /// <summary>
    /// Adds a PowerShell runspace pool resource to the distributed application.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="languageMode"></param>
    /// <param name="minRunspaces"></param>
    /// <param name="maxRunspaces"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<PowerShellRunspacePoolResource> AddPowerShell(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        PSLanguageMode languageMode = PSLanguageMode.ConstrainedLanguage,
        int minRunspaces = 1,
        int maxRunspaces = 5)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

        if (builder.Resources.OfType<PowerShellRunspacePoolResource>().Any(
            rsp => rsp.Name == name))
        {
            throw new DistributedApplicationException("AddPowerShell failed",
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "A PowerShell resource with the name '{0}' already exists.", name)));
        }

        var pool = new PowerShellRunspacePoolResource(name, languageMode, minRunspaces, maxRunspaces);

        builder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>(async (e, ct) =>
        {
            var pools = e.Model.Resources.OfType<PowerShellRunspacePoolResource>().ToList();

            foreach (var poolResource in pools)
            {
                Debug.Assert(poolResource is not null);

                var loggerService = e.Services.GetRequiredService<ResourceLoggerService>();
                var notificationService = e.Services.GetRequiredService<ResourceNotificationService>();

                var sessionState = InitialSessionState.CreateDefault();

                foreach (var annotation in poolResource.Annotations)
                {
                    if (annotation is PowerShellVariableReferenceAnnotation<ConnectionStringReference> reference)
                    {
                        var connectionString = await reference.Value.Resource.GetConnectionStringAsync(ct);
                        sessionState.Variables.Add(
                            new SessionStateVariableEntry(reference.Name, connectionString,
                                $"ConnectionString for {reference.Value.Resource.GetType().Name} '{reference.Name}'",
                                ScopedItemOptions.ReadOnly | ScopedItemOptions.AllScope));
                    }
                }

                var poolName = poolResource.Name;
                var poolLogger = loggerService.GetLogger(poolName);

                _ = notificationService.WaitForDependenciesAsync(poolResource, ct)
                    .ContinueWith(
                        async _ =>
                            await poolResource.StartAsync(sessionState, notificationService, poolLogger, ct),
                        ct);
            }
        });

        return builder.AddResource(pool)
            .WithInitialState(new()
            {
                ResourceType = "PowerShellRunspacePool",
                State = KnownResourceStates.NotStarted,
                Properties = [

                    new ("LanguageMode", pool.LanguageMode.ToString()),
                    new ("MinRunspaces", pool.MinRunspaces.ToString()),
                    new ("MaxRunspaces", pool.MaxRunspaces.ToString())
                ]
            })
            .ExcludeFromManifest();
    }
}
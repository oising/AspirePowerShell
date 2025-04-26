using System.Management.Automation;

namespace AspirePowerShell.AppHost;

/// <summary>
/// Extensions for the PowerShellRunspacePoolResourceBuilder.
/// </summary>
public static class PowerShellRunspacePoolResourceBuilderExtensions
{
    /// <summary>
    /// Adds a PowerShell script resource to the distributed application.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <param name="script"></param>
    /// <returns></returns>
    public static IResourceBuilder<PowerShellScriptResource> AddScript(
        this IResourceBuilder<PowerShellRunspacePoolResource> builder,
        [ResourceName] string name,
        string script)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(script);

        // parse to force an early exception if the script is invalid
        var scriptBlock = ScriptBlock.Create(script);

        var resource = new PowerShellScriptResource(name, scriptBlock, builder.Resource);

        return builder.ApplicationBuilder
            .AddResource(resource)
            .WaitFor(builder) // wait for pool resource
            .WithParentRelationship(builder.Resource) // owned by pool
            .WithInitialState(new()
            {
                ResourceType = "PowerShellScript",
                State = KnownResourceStates.NotStarted,
                Properties = [
                    new ("Script", script),
                    new("RunspacePool", builder.Resource.Name)
                ]
            })
            .ExcludeFromManifest();
    }

    /// <summary>
    /// Adds a reference to an Aspire resource that implements IResourceWithConnectionString.
    /// The resource will be exposed as a PowerShell variable in the runspace that is named after the resource name.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="source"></param>
    /// <param name="connectionName"></param>
    /// <param name="optional"></param>
    /// <returns></returns>
    public static IResourceBuilder<PowerShellRunspacePoolResource> WithReference(this IResourceBuilder<PowerShellRunspacePoolResource> builder, IResourceBuilder<IResourceWithConnectionString> source, string? connectionName = null, bool optional = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        var resource = source.Resource;

        builder.WithReferenceRelationship(resource);

        return builder.WithAnnotation(new PowerShellVariableReferenceAnnotation<ConnectionStringReference>(
            resource.Name, new ConnectionStringReference(resource, optional)));
    }
}

/// <summary>
/// Represents a PowerShell variable reference annotation.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="Name"></param>
/// <param name="Value"></param>
public record PowerShellVariableReferenceAnnotation<T>(string Name, T Value) : IResourceAnnotation;
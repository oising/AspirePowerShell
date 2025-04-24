using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace AspirePowerShell.AppHost;

public static class PowerShellRunspacePoolResourceBuilderExtensions
{
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

public record PowerShellVariableReferenceAnnotation<T>(string Name, T Value) : IResourceAnnotation;



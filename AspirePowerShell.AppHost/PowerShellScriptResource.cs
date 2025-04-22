using System.Diagnostics;
using System.Management.Automation;

namespace AspirePowerShell.AppHost
{
    public class PowerShellScriptResource : Resource, IDisposable,
        IResourceWithParent<PowerShellRunspacePoolResource>,
        IResourceWithEnvironment,
        IResourceWithWaitSupport
    {
        private readonly PowerShell _ps;

        public PowerShellScriptResource([ResourceName] string name, ScriptBlock script, PowerShellRunspacePoolResource parent) : base(name)
        {
            Parent = parent;
            _ps = PowerShell.Create();
            _ps.RunspacePool = parent.Pool;
        }

        public PowerShellRunspacePoolResource Parent { get; }

        public void Dispose()
        {
            _ps.Dispose();
        }
    }

    public static class ResourceBuilderExtensions
    {
        public static IResourceBuilder<PowerShellScriptResource> AddScript(
            this IResourceBuilder<PowerShellRunspacePoolResource> builder,
            [ResourceName] string name,
            string script)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

            if (string.IsNullOrWhiteSpace(script))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(script));

            var scriptBlock = ScriptBlock.Create(script);

            var resource = new PowerShellScriptResource(name, scriptBlock, builder.Resource);

            return builder.ApplicationBuilder.AddResource(resource)
                .WithInitialState(new()
                {
                    ResourceType = "PowerShellScript",
                    State = KnownResourceStates.NotStarted,
                    Properties = [
                        new ("Script", script),
                        new("RunspacePool", builder.Resource.Name)
                    ]
                });
        }
    }
}

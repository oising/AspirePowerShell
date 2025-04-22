using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using Azure.Provisioning.KeyVault;
using Microsoft.PowerShell;

namespace AspirePowerShell.AppHost;

public class PowerShellRunspacePoolResource : Resource, IDisposable
{
    public PSLanguageMode LanguageMode { get; }
    public int MinRunspaces { get; }
    public int MaxRunspaces { get; }

    public RunspacePool Pool { get; }

    public PowerShellRunspacePoolResource(
        [ResourceName] string name,
        PSLanguageMode languageMode = PSLanguageMode.ConstrainedLanguage,
        int minRunspaces = 1,
        int maxRunspaces = 5) : base(name)
    {
        LanguageMode = languageMode;
        MinRunspaces = minRunspaces;
        MaxRunspaces = maxRunspaces;

        var state = InitialSessionState.CreateDefault2();
        //state.AuthorizationManager = new AuthorizationManager("Aspire");
        //state.ImportPSModule("Az");
        state.LanguageMode = languageMode;

        Pool = RunspaceFactory.CreateRunspacePool(MinRunspaces, MaxRunspaces, state, new AspirePSHost());
    }

    private class AspirePSHost: PSHost
    {
        public override void SetShouldExit(int exitCode)
        {
            throw new NotImplementedException();
        }

        public override void EnterNestedPrompt()
        {
            throw new NotSupportedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotSupportedException();
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public override string Name { get; } = "AspirePSHost";
        public override Version Version { get; } = new (0, 1);
        public override Guid InstanceId { get; } = Guid.NewGuid();
        public override PSHostUserInterface UI { get; }
        public override CultureInfo CurrentCulture { get; } = CultureInfo.CurrentCulture;
        public override CultureInfo CurrentUICulture { get; } = CultureInfo.CurrentUICulture;
    }

    public void Dispose()
    {
        Pool.Close();
        Pool.Dispose();
    }
}

public static class PowerShellResourceBuilderExtensions
{
    public static IResourceBuilder<PowerShellRunspacePoolResource> WithModule(
        this IResourceBuilder<PowerShellRunspacePoolResource> builder,
        string moduleName)
    {
        // NOP
        return builder;
    }
}

using System.Globalization;
using System.Management.Automation;
using Aspire.Hosting.Lifecycle;

namespace AspirePowerShell.AppHost;

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

        if (builder.Resources.OfType<PowerShellRunspacePoolResource>().Any(r => r.Name == name))
        {
            throw new DistributedApplicationException("AddPowerShell failed",
                new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "A PowerShell resource with the name '{0}' already exists.", name)));
        }

        builder.Services.TryAddLifecycleHook<PowerShellRunspacePoolLifecycleHook>();
        builder.Services.TryAddLifecycleHook<PowerShellScriptLifecycleHook>();

        var pool = new PowerShellRunspacePoolResource(name, languageMode, minRunspaces, maxRunspaces);

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
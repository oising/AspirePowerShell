using System.Runtime.CompilerServices;

namespace AspirePowerShell.AppHost;


public static class PowerShellScriptResourceBuilderExtensions
{
    /// <summary>
    /// Provide arguments to the PowerShell script.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static IResourceBuilder<PowerShellScriptResource> WithArgs(
        this IResourceBuilder<PowerShellScriptResource> builder, params object[] args)
    {
        return builder.WithAnnotation(new PowerShellScriptArgsAnnotation(args));
    }
}

public record PowerShellScriptArgsAnnotation(object[] Args) : IResourceAnnotation;


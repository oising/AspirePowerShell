# Aspire PowerShell Scripting

[![Release to NuGet](https://github.com/oising/AspirePowerShell/actions/workflows/release.yml/badge.svg)](https://github.com/oising/AspirePowerShell/actions/workflows/release.yml)

## Installation

Add the NuGet package https://www.nuget.org/packages/Nivot.Aspire.Hosting.PowerShell to your Aspire AppHost project.

## About

Script your resources, use the pwsh (powershell core) engine and reference connectionstring expressions, live resources, dotnet instances or whatever else is in scope for your AppHost. 


```csharp
using AspirePowerShell.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ConsoleApp1>("consoleapp1");

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blob = storage.AddBlobs("myblob");

var ps = builder.AddPowerShell("ps")
    .WithReference(blob)
    .WaitFor(storage);

var script = ps.AddScript("script1", """
    param($x, $y)

    write-information "Hello, world"
    write-warning "This is a warning that $x + $y = $($x+ $y)"

    # uncommenting this will hang the script if you don't subsequently attach the pwsh debugger
    # wait-debugger

    # automatic variable from WithReference(blob)
    write-information "`$myblob is $myblob"

    az storage container create --connection-string $myblob -n demo
    az storage blob upload --connection-string $myblob -c demo --file ..\README.md

    write-information $pwd

    write-information "Blob uploaded"
""").WithArgs(2, 2); // param($x, $y)

builder.Build().Run();
```

## Debugging

While your Apphost is running a script that is waiting via `Wait-Debugger`, open a terminal with powershell (pwsh) 7.4 or later (win, osx, linux) and use `Get-PSHostProcessInfo`, `Enter-PSHostProcess`, `Get-Runspace` and `Debug-Runspace` to connect the debugger. 

See https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/enter-pshostprocess?view=powershell-7.5 for more information.

## Intellisense

JetBrains Rider (sorry, doesn't work in Visual Studio) is able to provide hints as to the contexts of strings, so because the parameter is annotated with `[StringSyntax("PowerShell")]`, if the PowerShell plugin is installed you'll get language support:


![image](https://github.com/user-attachments/assets/9c65528d-5afd-4aeb-a08b-f6597555dece)

Thanks to @akarpov89 at JetBrains for this tip!

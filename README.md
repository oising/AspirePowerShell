# Aspire PowerShell Scripting

## About

NOTE: This is not yet available as an Aspire Hosting package. This is a proof of concept, albeit a fairly solid one.

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
    write-information "Hello, world"

    # uncommenting this will hang the script if you don't attach the pwsh debugger
    # wait-debugger

    write-information "`$myblob is $myblob"
""");

builder.Build().Run();
```

## Debugging

While your Apphost is running a script that is waiting via `Wait-Debugger`, open a terminal with powershell (pwsh) 7.4 or later (win, osx, linux) and use `Get-PSHostProcessInfo`, `Enter-PSHostProcess`, `Get-Runspace` and `Debug-Runspace` to connect the debugger. 

See https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/enter-pshostprocess?view=powershell-7.5 for more information.

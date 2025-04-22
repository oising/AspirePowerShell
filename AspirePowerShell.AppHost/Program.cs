using System.Management.Automation;
using AspirePowerShell.AppHost;
using Microsoft.Extensions.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var ps = builder.AddPowerShell("ps")
    .WithModule("Az");

builder.AddProject<Projects.ConsoleApp1>("consoleapp1");

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blob = storage.AddBlobs("blob");

var script = ps.AddScript("script1", """
    write-host "Hello, world"
    wait-debugger
    write-host "blob is $blob"
""")
    .WithReference(blob)
    .WithParentRelationship(ps)
    .WaitFor(storage);

builder.Build().Run();


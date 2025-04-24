using AspirePowerShell.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ConsoleApp1>("consoleapp1");

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blob = storage.AddBlobs("blob");

var ps = builder.AddPowerShell("ps")
    .WithReference(blob)
    .WaitFor(storage);

var script = ps.AddScript("script1", """
    write-information "Hello, world"

    wait-debugger

    write-information "blob is $blob"
""");

builder.Build().Run();


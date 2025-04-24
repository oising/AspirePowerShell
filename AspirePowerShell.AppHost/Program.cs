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


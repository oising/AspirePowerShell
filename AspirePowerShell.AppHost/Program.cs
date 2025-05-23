using Nivot.Aspire.Hosting.PowerShell;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blob = storage.AddBlobs("myblob");

var ps = builder.AddPowerShell("ps")
    .WithReference(blob)
    .WaitFor(storage);

var script1 = ps.AddScript("script1", """
    param($x, $y)

    write-information "Hello, world"
    write-warning "This is a warning that $x + $y = $($x+ $y)"

    # uncommenting this will hang the script if you don't attach the pwsh debugger
    # wait-debugger

    write-information "`$myblob is $myblob"

    az storage container create --connection-string $myblob -n demo
    az storage blob upload --connection-string $myblob -c demo --file ..\README.md

    write-information $pwd

    write-information "Blob uploaded"
""").WithArgs(2, 2);

var script2 = ps.AddScript("script2", """
    write-information "Hello, world from script2"
    """)
    .WaitForCompletion(script1);

builder.Build().Run();


using System.Diagnostics;
using System.Management.Automation;
using Aspire.Hosting.ApplicationModel;
using Humanizer.Localisation;
using Microsoft.Extensions.Logging;

namespace AspirePowerShell.AppHost
{
    public class PowerShellScriptResource : Resource, IDisposable,
        IResourceWithEnvironment,
        IResourceWithWaitSupport,
        IResourceWithArgs
    {
        private readonly PowerShell _ps;
        private readonly CancellationTokenSource _cts;
        private readonly PowerShellRunspacePoolResource _parent;
        private readonly PSDataCollection<PSObject> _output;
        private readonly PSDataCollection<PSObject> _emptyInput;

        public PowerShellScriptResource([ResourceName] string name, ScriptBlock script, PowerShellRunspacePoolResource parent) : base(name)
        {
            _parent = parent;
            _cts = new CancellationTokenSource();
            _ps = PowerShell.Create();

            _output = new PSDataCollection<PSObject>();
            _emptyInput = new PSDataCollection<PSObject>();
            _emptyInput.Complete();

            _ps.AddScript(script.ToString());
        }

        public async Task StartAsync(ILogger scriptLogger,
            ResourceNotificationService notificationService,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(scriptLogger != null);

            Debug.Assert(_parent.Pool != null);
            _ps.RunspacePool = _parent.Pool;

            ConfigurePSDataStreams(scriptLogger, notificationService);

            _ps.InvocationStateChanged += async (sender, args) =>
            {
                var knownState = args.InvocationStateInfo.State switch
                {
                    PSInvocationState.NotStarted => KnownResourceStates.NotStarted,
                    PSInvocationState.Running => KnownResourceStates.Running,
                    PSInvocationState.Completed => KnownResourceStates.Finished,
                    PSInvocationState.Stopped => KnownResourceStates.Exited,
                    PSInvocationState.Failed => KnownResourceStates.FailedToStart,
                    PSInvocationState.Stopping => KnownResourceStates.Stopping,
                    _ => throw new ArgumentOutOfRangeException( // probably should be assertion
                        nameof(args.InvocationStateInfo.State),
                        args.InvocationStateInfo.State,
                        "Unknown PowerShell invocation state")
                };

                await notificationService.PublishUpdateAsync(this,
                    state => state with
                {
                    State = knownState,
                    Properties = [.. state.Properties,
                        new( "PSInvocationState", args.InvocationStateInfo.State.ToString() ),
                        new( "Reason", args.InvocationStateInfo.Reason?.Message ?? string.Empty ),
                    ],
                });
            };
            
            if (this.TryGetLastAnnotation<PowerShellScriptArgsAnnotation>(out var scriptArgsAnnotation))
            {
                foreach (var scriptArg in scriptArgsAnnotation.Args)
                {
                    if (scriptArg is IValueProvider valueProvider)
                    {
                        var value = await valueProvider.GetValueAsync(cancellationToken);
                        _ps.AddArgument(value);
                    }
                    else
                    {
                        _ps.AddArgument(scriptArg);
                    }
                }
            }

            // Use Task.Factory.FromAsync to convert the APM pattern to Task
            await Task.Factory.FromAsync(
                _ps.BeginInvoke(_emptyInput, _output),
                asyncResult => {
                    _ = _ps.EndInvoke(asyncResult);
                });
        }

        private void ConfigurePSDataStreams(ILogger logger, ResourceNotificationService notifications)
        {
            _output.DataAdded += (sender, args) =>
            {
                var psObject = _output[args.Index];
                logger.LogInformation("Output: {Output}", psObject.ToString());
            };
            _output.Completed += async (sender, args) =>
            {
                await notifications.PublishUpdateAsync(this, state => state with
                {
                    State = KnownResourceStates.Finished,
                    StopTimeStamp = DateTime.Now,
                    ExitCode = _ps.HadErrors ? 1 : 0,
                });

                logger.LogInformation("Output completed");
            };
            _ps.Streams.Error.DataAdded += (sender, args) =>
            {
                var error = _ps.Streams.Error[args.Index];
                logger.LogError(error.Exception, "Error: {Error}", error.ToString());
            };
            _ps.Streams.Warning.DataAdded += (sender, args) =>
            {
                var warning = _ps.Streams.Warning[args.Index];
                logger.LogWarning("Warning: {Warning}", warning);
            };
            _ps.Streams.Information.DataAdded += (sender, args) =>
            {
                var info = _ps.Streams.Information[args.Index];
                logger.LogInformation("Information: {Info}", info);
            };
            _ps.Streams.Verbose.DataAdded += (sender, args) =>
            {
                var verbose = _ps.Streams.Verbose[args.Index];
                logger.LogInformation("Verbose: {Verbose}", verbose);
            };
            _ps.Streams.Debug.DataAdded += (sender, args) =>
            {
                var debug = _ps.Streams.Debug[args.Index];
                logger.LogInformation("Debug: {Debug}", debug);
            };
        }

        void IDisposable.Dispose()
        {
            _ps.Stop();
            _ps.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

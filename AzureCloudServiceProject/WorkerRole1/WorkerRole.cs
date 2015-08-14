using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using AzureStorageManagerLibrary;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private AzureStorageManager asManager;

        public override async void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");

            //Take items from queue
            await asManager.runQueue();

            base.Run();
        }

        public override bool OnStart()
        {
            //Create Manager
            asManager = new AzureStorageManager(RoleEnvironment.GetConfigurationSettingValue(Constants.stringName));

            ServicePointManager.DefaultConnectionLimit = 12;

            Trace.TraceInformation("WorkerRole has been started");

            return base.OnStart();
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}

using System;
using System.Globalization;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace get_twin
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            WriteLineWithTimestamp("get-twin Main()\n");

            (CancellationTokenSource cts, ManualResetEventSlim completed) = InitShutdownHandler();

            try
            {
                WriteLineWithTimestamp("Connecting to edge hub...\n");
                ModuleClient moduleClient = await ModuleClient.CreateFromEnvironmentAsync();
                await moduleClient.OpenAsync(cts.Token);
                WriteLineWithTimestamp("Connected.\n");

                while (!cts.Token.IsCancellationRequested)
                {
                    WriteLineWithTimestamp("Requesting module twin...\n");
                    var twin = await moduleClient.GetTwinAsync(cts.Token);
                    WriteLineWithTimestamp($"Received module twin:\n{twin.ToJson(Formatting.Indented)}\n");
                    await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
            }

            completed.Set();
            return 0;
        }

        static (CancellationTokenSource, ManualResetEventSlim) InitShutdownHandler()
        {
            var cts = new CancellationTokenSource();
            var completed = new ManualResetEventSlim();

            void OnUnload(AssemblyLoadContext ctx) => CancelProgram();

            void CancelProgram()
            {
                WriteLineWithTimestamp("\nTermination requested, initiating shutdown");
                cts.Cancel();
                WriteLineWithTimestamp("Waiting for cleanup to finish...");
                if (completed.Wait(TimeSpan.FromMinutes(1)))
                {
                    WriteLineWithTimestamp("Done with cleanup, shutting down");
                }
                else
                {
                    WriteLineWithTimestamp("Timed out waiting for cleanup to finish, shutting down");
                }
            }

            AssemblyLoadContext.Default.Unloading += OnUnload;
            Console.CancelKeyPress += (sender, cpe) => CancelProgram();
            WriteLineWithTimestamp("Listening for shutdown request...");

            return (cts, completed);
        }

        static void WriteLineWithTimestamp(string message)
        {
            string timestamp = DateTime.UtcNow.ToString(
                "yyyy-MM-dd HH:mm:ss.fff zzz",
                CultureInfo.InvariantCulture);
            Console.WriteLine($"{timestamp} {message}");
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace i_need_internet
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            WriteLineWithTimestamp("i-need-internet Main()\n");

            (CancellationTokenSource cts, ManualResetEventSlim completed) = InitShutdownHandler();

            const string url = @"https://raw.githubusercontent.com/Azure/iotedge/master/LICENSE";

            while (!cts.Token.IsCancellationRequested)
            {
                WriteLineWithTimestamp($"Requesting GET '{url}'...\n");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Console.WriteLine(await reader.ReadToEndAsync().WithCancellation(cts.Token));
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), cts.Token);
                }
                catch (TaskCanceledException)
                {
                }
                catch
                {
                    WriteLineWithTimestamp("Exception thrown, shutting down");
                    throw;
                }
            }

            completed.Set();
            return 0;
        }

        static void WriteLineWithTimestamp(string message)
        {
            string timestamp = DateTime.UtcNow.ToString(
                "yyyy-MM-dd HH:mm:ss.fff zzz",
                CultureInfo.InvariantCulture);
            Console.WriteLine($"{timestamp} {message}");
        }

        static (CancellationTokenSource, ManualResetEventSlim) InitShutdownHandler()
        {
            var cts = new CancellationTokenSource();
            var completed = new ManualResetEventSlim();

            void OnUnload(AssemblyLoadContext ctx) => CancelProgram();

            void CancelProgram()
            {
                Console.WriteLine("\nTermination requested, initiating shutdown");
                cts.Cancel();
                Console.WriteLine("Waiting for cleanup to finish...");
                if (completed.Wait(TimeSpan.FromMinutes(1)))
                {
                    Console.WriteLine("Done with cleanup, shutting down");
                }
                else
                {
                    Console.WriteLine("Timed out waiting for cleanup to finish, shutting down");
                }
            }

            AssemblyLoadContext.Default.Unloading += OnUnload;
            Console.CancelKeyPress += (sender, cpe) => CancelProgram();
            Console.WriteLine("Listening for shutdown request...");

            return (cts, completed);
        }
    }

    static class StreamReaderExt
    {
        // https://stackoverflow.com/a/28626769/4195001
        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return task.IsCompleted
                ? task
                : task.ContinueWith(
                    completedTask => completedTask.GetAwaiter().GetResult(),
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }
    }
}

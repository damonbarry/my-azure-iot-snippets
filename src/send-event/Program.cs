using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace send_event
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var secretUri = args.ElementAtOrDefault(0);
            var transport = args.ElementAtOrDefault(1);

            if (string.IsNullOrWhiteSpace(secretUri))
            {
                Console.Error.WriteLine("Missing argument for secretUri");
                PrintUsage();
                return 1;
            }

            if (string.IsNullOrWhiteSpace(transport) || !Parse(transport, out TransportType transportType))
            {
                Console.Error.WriteLine("Invalid or missing argument for transport");
                PrintUsage();
                return 1;
            }

            var secret = await GetSecretAsync(secretUri);
            var builder = IotHubConnectionStringBuilder.Create(secret);
            var connectionString = builder.ToString();

            var message = $"{DateTime.UtcNow} Hello world!";

            if (string.IsNullOrEmpty(builder.ModuleId))
            {
                Console.Write($"Sending an event as {builder.DeviceId}: {message}...");
                var client = DeviceClient.CreateFromConnectionString(connectionString, transportType);
                await client.SendEventAsync(new Message(Encoding.ASCII.GetBytes(message)));
            }
            else
            {
                Console.Write($"Sending an event as {builder.DeviceId}/{builder.ModuleId}: {message}...");
                var client = ModuleClient.CreateFromConnectionString(connectionString, transportType);
                await client.SendEventAsync(new Message(Encoding.ASCII.GetBytes(message)));
            }

            Console.WriteLine("done");

            return 0;
        }

        static async Task<string> GetSecretAsync(string uri)
        {
            var provider = new AzureServiceTokenProvider();
            var kv = new KeyVaultClient(
                new KeyVaultClient.AuthenticationCallback(provider.KeyVaultTokenCallback));
            var secret = await kv.GetSecretAsync(uri).ConfigureAwait(false);
            return secret.Value;
        }

        static bool Parse(string value, out TransportType transport)
        {
            switch (value.ToLowerInvariant())
            {
                case "amqp":
                    transport = TransportType.Amqp_Tcp_Only;
                    return true;
                case "mqtt":
                    transport = TransportType.Mqtt_Tcp_Only;
                    return true;
                default:
                    transport = default(TransportType);
                    return false;
            }
        }

        static void PrintUsage()
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("send-event {secretUri} {transport}");
            Console.Error.WriteLine("\tsecretUri\tURI to an Azure Key Vault secret containing a edge device (or module) connection string");
            Console.Error.WriteLine("\ttransport\tThe transport to use when connecting to IoT Hub; AMQP or MQTT (case insensitive)");
            Console.Error.WriteLine();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Producer.Models.Messages;
using Producer.Serialization;
using Producer.Services;

namespace Producer
{
    internal class Program
    {
        private static async Task Main()
        {
            var devVariable = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
            var amountOfProducersVariable = Environment.GetEnvironmentVariable("PRODUCER_AMOUNT") ?? "2";
            var amountOfMessagesVariable = Environment.GetEnvironmentVariable("MESSAGE_AMOUNT") ?? "1000";

            var message = GenerateMessages(int.Parse(amountOfMessagesVariable));
            var producers = GetProducers(int.Parse(amountOfProducersVariable), string.IsNullOrEmpty(devVariable));

            producers.ForEach(async producer => { await producer.ConnectToBroker(); });
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => producers.ForEach(async producer => { await producer.CloseConnection(); });

            while (true)
            {
                producers.ForEach(async producer =>
                {
                    await producer.SendMessage(message);
                });

                await Task.Delay(1000);
            }
        }

        public static List<ProducerService> GetProducers(int amount, bool isDev)
        {
            var list = new List<ProducerService>();

            for (var i = 0; i < amount; i++)
            {
                var connectionString = isDev ? "ws://localhost:5000/ws" : $"ws://broker-{i}.broker.default.svc.cluster.local/ws";
                list.Add(new ProducerService(new Serializer(), new ProducerSocket(), connectionString));
            }

            return list;
        }

        public static Message GenerateMessages(int amount)
        {
            var messages = new string[amount];
            for (var i = 0; i < amount; i++)
            {
                messages[i] = $"Hello World {i}";
            }

            return new Message
            {
                Msg = messages
            };
        }
    }
}

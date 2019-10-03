using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var batchingSizeVariable = Environment.GetEnvironmentVariable("BATCHING_SIZE") ?? "23";
            var partitionAmountVariable = Environment.GetEnvironmentVariable("PARTITION_AMOUNT") ?? "100";

            var producers = GetProducers(int.Parse(amountOfProducersVariable), string.IsNullOrEmpty(devVariable), int.Parse(batchingSizeVariable));

            producers.ForEach(async producer => { await producer.ConnectToBroker(); });
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => producers.ForEach(async producer => { await producer.CloseConnection(); });

            while (true)
            {
                var (messageHeader, message) = MessageGenerator.GenerateMessages(int.Parse(amountOfMessagesVariable), long.Parse(partitionAmountVariable));

                producers.ForEach(async producer =>
                {
                    for (var i = 0; i < messageHeader.Length; i++)
                    {
                        await producer.AddMessage(messageHeader[i], message[i]);
                    }
                });

                await Task.Delay(1000);
            }
        }

        public static List<ProducerService> GetProducers(int amount, bool isDev, int batchingSize)
        {
            var list = new List<ProducerService>();

            for (var i = 0; i < amount; i++)
            {
                var connectionString = isDev ? "ws://localhost:5000/ws" : $"ws://broker-{i}.broker.default.svc.cluster.local/ws";
                list.Add(new ProducerService(new Serializer(), new ProducerSocket(), new BatchingService(batchingSize), connectionString));
            }

            return list;
        }
    }
}

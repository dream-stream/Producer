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
            var devVariable = EnvironmentVariables.DevVariable;
            var amountOfProducersVariable = EnvironmentVariables.AmountOfProducersVariable;
            var amountOfMessagesVariable = EnvironmentVariables.AmountOfMessagesVariable;
            var batchingSizeVariable = EnvironmentVariables.BatchingSizeVariable;
            var partitionAmountVariable = EnvironmentVariables.PartitionAmountVariable;

            var producers = GetProducers(amountOfProducersVariable, string.IsNullOrEmpty(devVariable), batchingSizeVariable); //TODO Lav producer til tråde

            producers.ForEach(async producer => { await producer.ConnectToBroker(); });
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => producers.ForEach(async producer => { await producer.CloseConnection(); });

            while (true)
            {
                producers.ForEach(async producer =>
                {
                    var (messageHeader, message) = MessageGenerator.GenerateMessages(amountOfMessagesVariable, partitionAmountVariable);

                    for (var i = 0; i < messageHeader.Length; i++)
                    {
                        await producer.AddMessage(messageHeader[i], message[i]);
                    }
                });

                await Task.Delay(15*1000);
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

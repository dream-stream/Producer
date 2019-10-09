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

            var producers = await GetProducers(amountOfProducersVariable, string.IsNullOrEmpty(devVariable), batchingSizeVariable); //TODO Turn producers into threads

            AppDomain.CurrentDomain.ProcessExit += (sender, e) => producers.ForEach(async producer => { await producer.CloseConnection(); });

            while (true)
            {
                producers.ForEach(async producer =>
                {
                    var (messageHeader, message) = MessageGenerator.GenerateMessages(amountOfMessagesVariable, partitionAmountVariable);

                    for (var i = 0; i < messageHeader.Length; i++)
                    {
                        await producer.Publish(messageHeader[i], message[i]);
                    }
                });

                await Task.Delay(15*1000); //Delay added for test of timer on batches
            }
        }

        public static async Task<List<IProducer>> GetProducers(int amount, bool isDev, int batchingSize)
        {
            var list = new List<IProducer>();

            for (var i = 0; i < amount; i++)
            {
                var connectionString = isDev ? "ws://localhost:5000/ws" : $"ws://broker-{i}.broker.default.svc.cluster.local/ws";
                
                list.Add(new ProducerService(new Serializer(), new ProducerSocket(),
                    new BatchingService(batchingSize)));

                await list[i].Connect(connectionString);
            }

            return list;
        }
    }
}

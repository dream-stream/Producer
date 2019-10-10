using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dotnet_etcd;
using Producer.Serialization;
using Producer.Services;

namespace Producer
{
    internal class Program
    {
        private static async Task Main()
        {
            //var devVariable = EnvironmentVariables.DevVariable;
            //var amountOfProducersVariable = EnvironmentVariables.AmountOfProducersVariable;
            var amountOfMessagesVariable = EnvironmentVariables.AmountOfMessagesVariable;
            var batchingSizeVariable = EnvironmentVariables.BatchingSizeVariable;
            var partitionAmountVariable = EnvironmentVariables.PartitionAmountVariable;

            var producer = new ProducerService(new Serializer(), new BatchingService(batchingSizeVariable), partitionAmountVariable);
            var client = EnvironmentVariables.IsDev ? new EtcdClient("http://localhost") : new EtcdClient("http://etcd");
            await producer.InitSockets(client);

            AppDomain.CurrentDomain.ProcessExit += async (sender, e) => await producer.CloseConnections();

            while (true)
            {
                var (messageHeaders, messages) = MessageGenerator.GenerateMessages(amountOfMessagesVariable, partitionAmountVariable);

                for (var i = 0; i < messageHeaders.Length; i++)
                {
                    await producer.Publish(messageHeaders[i], messages[i]);
                }

                await Task.Delay(15*1000); //Delay added for test of timer on batches
            }
        }

        //public static async Task<List<IProducer>> GetProducers(int amount, bool isDev, int batchingSize)
        //{
        //    var list = new List<IProducer>();

        //    for (var i = 0; i < amount; i++)
        //    {
        //        var connectionString = isDev ? "ws://localhost:5000/ws" : $"ws://broker-{i}.broker.default.svc.cluster.local/ws";
                
        //        list.Add(new ProducerService(new Serializer(), new BrokerSocket(),
        //            new BatchingService(batchingSize)));

        //        await list[i].Connect(connectionString);
        //    }

        //    return list;
        //}
    }
}

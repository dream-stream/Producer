using System;
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
            Console.WriteLine("This is the new version 3");
            var amountOfMessagesVariable = EnvironmentVariables.AmountOfMessagesVariable;
            var batchingSizeVariable = EnvironmentVariables.BatchingSizeVariable;
            var partitionAmountVariable = EnvironmentVariables.PartitionAmountVariable;

            var producer = new ProducerService(new Serializer(), new BatchingService(batchingSizeVariable));
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
    }
}

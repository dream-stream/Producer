using System;
using System.Threading.Tasks;
using dotnet_etcd;
using Producer.Serialization;
using Producer.Services;
using Prometheus;

namespace Producer
{
    internal class Program
    {
        private static async Task Main()
        {

            Console.WriteLine("This is the new version 3");
            var amountOfMessagesVariable = EnvironmentVariables.AmountOfMessagesVariable;
            var batchingSizeVariable = EnvironmentVariables.BatchingSizeVariable;

            var producer = new ProducerService(new Serializer(), new BatchingService(batchingSizeVariable));
            
            const string topic = "Topic2";
            int partitionCount;
            if (EnvironmentVariables.IsDev)
            {
                await producer.InitSocketLocalhost();
                partitionCount = 10;
            }
            else
            {
                var metricServer = new MetricServer(80);
                metricServer.Start();
                var client = EnvironmentVariables.IsDev ? new EtcdClient("http://localhost") : new EtcdClient("http://etcd");
                await producer.InitSockets(client);
                partitionCount = await TopicList.GetPartitionCount(client, topic);
            }

            AppDomain.CurrentDomain.ProcessExit += async (sender, e) => await producer.CloseConnections();
            while (true)
            {
                var (messageHeaders, messages) = MessageGenerator.GenerateMessages(amountOfMessagesVariable, partitionCount, topic);

                for (var i = 0; i < messageHeaders.Length; i++)
                {
                    await producer.Publish(messageHeaders[i], messages[i]);
                }

                await Task.Delay(15 * 1000); //Delay added for test of timer on batches
            }
        }
    }
}

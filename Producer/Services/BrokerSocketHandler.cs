using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_etcd;
using Mvccpb;
using Prometheus;

namespace Producer.Services
{
    public class BrokerSocketHandler
    {
        private static readonly Gauge PartitionCounterGauge = Metrics
            .CreateGauge("partition_counter_gauge", "Number of partitions assigned to each broker local.", new GaugeConfiguration
            {
                LabelNames = new []{"Broker"}
            });


        public const string TopicTablePrefix = "Topic/";
        public const string BrokerTablePrefix = "Broker/";

        public static async Task<BrokerSocket[]> UpdateBrokerSockets(EtcdClient client, BrokerSocket[] brokerSockets)
        {
            var rangeResponse = await client.GetRangeValAsync(BrokerTablePrefix);
            var maxBrokerNumber = rangeResponse.Keys.Max(GetBrokerNumber);
            brokerSockets = new BrokerSocket[maxBrokerNumber + 1];
            foreach (var (key, _) in rangeResponse) await AddBroker(key, brokerSockets);
            return brokerSockets;
        }

        public static async Task UpdateBrokerSocketsDictionary(EtcdClient client, Dictionary<string, BrokerSocket> brokerSocketsDict, BrokerSocket[] brokerSockets)
        {
            var rangeVal = await client.GetRangeValAsync(TopicTablePrefix);
            foreach (var (key, value) in rangeVal) AddToBrokerSocketsDictionary(brokerSocketsDict, brokerSockets, key, value);

            UpdatePartitionCounterGauge(brokerSocketsDict);
            PrintBrokerSocketsDict(brokerSocketsDict);
        }

        private static void PrintBrokerSocketsDict(Dictionary<string, BrokerSocket> brokerSocketsDict)
        {
            Console.WriteLine("PrintBrokerSocketsDict");
            foreach (var kv in brokerSocketsDict)
            {
                Console.WriteLine($"Key: {kv.Key}, value: {kv.Value.ConnectedTo}");
            }
        }

        // Anders shit code to count :)
        private static void UpdatePartitionCounterGauge(Dictionary<string, BrokerSocket> brokerSocketsDict)
        {
            var partitionCountForBroker = new Dictionary<string, int>();
            foreach (var (_, value) in brokerSocketsDict)
            {
                if (partitionCountForBroker.TryGetValue(value.ConnectedTo, out _)) partitionCountForBroker[value.ConnectedTo]++;
                else partitionCountForBroker[value.ConnectedTo] = 1;
            }

            foreach (var labelName in PartitionCounterGauge.LabelNames)
            {
                PartitionCounterGauge.WithLabels(labelName).Set(0);
            }

            foreach (var (key, value) in partitionCountForBroker)
            {
                PartitionCounterGauge.WithLabels(key).Set(value);
            }
        }

        private static void AddToBrokerSocketsDictionary(IDictionary<string, BrokerSocket> brokerSocketsDict, BrokerSocket[] brokerSockets, string key, string value)
        {
            var topicAndPartition = key.Substring(TopicTablePrefix.Length);
            var brokerNumber = GetBrokerNumber(value);

            // Handling stuff race condition
            if (brokerNumber >= brokerSockets.Length)
            {
                Console.WriteLine($"UPS!!! brokerNumber larger than brokerSockets.Length, {brokerNumber} {brokerSockets.Length}");

                Console.WriteLine($"Ignoring!!!!!!! ");
                Console.WriteLine($"Current key: {topicAndPartition} value: {brokerSocketsDict[topicAndPartition]}, should have been updated with broker number {brokerNumber}, but array only contains: ");
                Array.ForEach(brokerSockets, Console.WriteLine);
            }
            else
            {
                brokerSocketsDict[topicAndPartition] = brokerSockets[brokerNumber];
            }
        }

        public static async Task BrokerTableChangedHandler(WatchEvent[] watchEvents, BrokerSocket[] brokerSockets)
        {
            foreach (var watchEvent in watchEvents)
            {
                switch (watchEvent.Type)
                {
                    case Event.Types.EventType.Put:
                        await AddBroker(watchEvent.Key, brokerSockets);
                        break;
                    case Event.Types.EventType.Delete:
                        await RemoveBroker(watchEvent, brokerSockets);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static void TopicTableChangedHandler(WatchEvent[] watchEvents, Dictionary<string, BrokerSocket> brokerSocketsDict, BrokerSocket[] brokerSockets)
        {
            foreach (var watchEvent in watchEvents)
            {
                switch (watchEvent.Type)
                {
                    case Event.Types.EventType.Put:
                            AddToBrokerSocketsDictionary(brokerSocketsDict, brokerSockets, watchEvent.Key, watchEvent.Value);
                            UpdatePartitionCounterGauge(brokerSocketsDict);
                            PrintBrokerSocketsDict(brokerSocketsDict);
                        break;
                    case Event.Types.EventType.Delete:
                        // Do nothing!!!
                        // todo At least for now we don't do this :D
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static async Task RemoveBroker(WatchEvent watchEvent, BrokerSocket[] brokerSockets)
        {
            var brokerNumber = GetBrokerNumber(watchEvent.Key);
            await brokerSockets[brokerNumber].CloseConnection();
            brokerSockets[brokerNumber] = null;
        }

        public static async Task AddBroker(string keyString, BrokerSocket[] brokerSockets)
        {
            var brokerNumber = GetBrokerNumber(keyString);
            var brokerName = GetBrokerName(keyString);
            if (brokerSockets.Length > brokerNumber) 
                await CreateStartAndAddBroker(brokerName, brokerNumber, brokerSockets);
            else
            {
                Array.Resize(ref brokerSockets, brokerNumber + 1);
                await CreateStartAndAddBroker(brokerName, brokerNumber, brokerSockets);
            }
        }

        private static async Task CreateStartAndAddBroker(string brokerName, int brokerNumber, BrokerSocket[] brokerSockets)
        {
            var brokerSocket = new BrokerSocket();
            var connectionString = EnvironmentVariables.IsDev ? "ws://localhost:5000/ws" : $"ws://{brokerName}.broker.default.svc.cluster.local/ws";
            await brokerSocket.ConnectToBroker(connectionString);
            brokerSockets[brokerNumber] = brokerSocket;
        }

        private static int GetBrokerNumber(string brokerString)
        {
            var brokerNumberString = brokerString.Split('-').Last();
            int.TryParse(brokerNumberString, out var brokerNumber);
            return brokerNumber;
        }

        private static string GetBrokerName(string brokerString)
        {
            return brokerString.Substring(BrokerTablePrefix.Length);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_etcd;
using Mvccpb;

namespace Producer.Services
{
    public class BrokerSocketHandler
    {
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
            brokerSockets = await UpdateBrokerSockets(client, brokerSockets); // Force update, since sometimes it has not been updated.
            foreach (var (key, value) in rangeVal) AddToBrokerSocketsDictionary(brokerSocketsDict, brokerSockets, key, value);

            // This gives an Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
            //PrintBrokerSocketsDict(brokerSocketsDict);
        }

        private static void PrintBrokerSocketsDict(Dictionary<string, BrokerSocket> brokerSocketsDict)
        {
            Console.WriteLine("PrintBrokerSocketsDict");
            foreach (var (key, value) in brokerSocketsDict)
            {
                Console.WriteLine($"Key: {key}, value: {value.ConnectedTo}");
            }
        }

        private static void AddToBrokerSocketsDictionary(IDictionary<string, BrokerSocket> brokerSocketsDict, BrokerSocket[] brokerSockets, string key, string value)
        {
            var topicAndPartition = key.Substring(TopicTablePrefix.Length);
            var brokerNumber = GetBrokerNumber(value);

            // Handling race condition
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

        public static async Task<BrokerSocket[]> BrokerTableChangedHandler(WatchEvent[] watchEvents, BrokerSocket[] brokerSockets)
        {
            Console.WriteLine("BrokerTableChangedHandler");
            foreach (var watchEvent in watchEvents)
            {
                Console.WriteLine($"BrokerTableChangedHandler 2 {watchEvent.Type}");
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

            return brokerSockets;
        }

        public static void TopicTableChangedHandler(WatchEvent[] watchEvents, Dictionary<string, BrokerSocket> brokerSocketsDict, BrokerSocket[] brokerSockets)
        {
            foreach (var watchEvent in watchEvents)
            {
                switch (watchEvent.Type)
                {
                    case Event.Types.EventType.Put:
                        Console.WriteLine("ANDERS!!!");
                        Array.ForEach(brokerSockets, socket =>
                        {
                            if(socket != null)
                                Console.WriteLine($"{socket.ConnectedTo} {socket.IsOpen()}");
                        });
                        AddToBrokerSocketsDictionary(brokerSocketsDict, brokerSockets, watchEvent.Key, watchEvent.Value);
                        // This gives an Unhandled exception. System.NullReferenceException: Object reference not set to an instance of an object.
                        //PrintBrokerSocketsDict(brokerSocketsDict);
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
            {
                await CreateStartAndAddBroker(brokerName, brokerNumber, brokerSockets);
                Console.WriteLine($"Added Broker {brokerName}\nBrokerSockets: ");
                Array.ForEach(brokerSockets, Console.WriteLine);
            }
            else
            {
                Array.Resize(ref brokerSockets, brokerNumber + 1);
                await CreateStartAndAddBroker(brokerName, brokerNumber, brokerSockets);
                Console.WriteLine($"Added Broker after resize {brokerName}\nBrokerSockets: ");
                Array.ForEach(brokerSockets, Console.WriteLine);
            }
        }

        private static async Task CreateStartAndAddBroker(string brokerName, int brokerNumber, BrokerSocket[] brokerSockets)
        {
            Console.WriteLine("CreateStartAndAddBroker");
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
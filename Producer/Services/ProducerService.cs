using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotnet_etcd;
using Producer.Models.Messages;
using Producer.Serialization;

namespace Producer.Services
{
    public class ProducerService : IProducer
    {
        private readonly ISerializer _serializer;
        private readonly BatchingService _batchingService;
        //private readonly BrokerSocket[] _producerSockets;
        private BrokerSocket[] _brokerSockets;
        private readonly Dictionary<string, BrokerSocket> _brokerSocketsDict = new Dictionary<string, BrokerSocket>();
        private EtcdClient _client;
        private const string BrokerTablePrefix = "Broker/";
        private const string TopicTablePrefix = "Topic/";

        public ProducerService(ISerializer serializer, BatchingService batchingService, long partitions)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
            //_producerSockets = new BrokerSocket[partitions];
        }

        public async Task InitSockets(EtcdClient client)
        {
            _client = client;
            await UpdateBrokerSockets(client);
            await UpdateBrokerSocketsDictionary(client);
        }

        private async Task UpdateBrokerSocketsDictionary(EtcdClient client)
        {
            var rangeResponse = await client.GetRangeAsync(TopicTablePrefix);
            foreach (var keyValue in rangeResponse.Kvs)
            {
                var topicAndPartition = keyValue.Key.ToStringUtf8().Substring(TopicTablePrefix.Length);
                var brokerNumberString = keyValue.Value.ToStringUtf8().Split('-').Last();
                int.TryParse(brokerNumberString, out var brokerNumber);
                _brokerSocketsDict[topicAndPartition] = _brokerSockets[brokerNumber];
            }
        }

        private async Task UpdateBrokerSockets(EtcdClient client)
        {
            var rangeResponse = await client.GetRangeAsync(BrokerTablePrefix);
            var maxBrokerNumberString = rangeResponse.Kvs.Max(kv => kv.Key.ToStringUtf8().Split('-').Last());
            int.TryParse(maxBrokerNumberString, out var maxBrokerNumber);
            _brokerSockets = new BrokerSocket[maxBrokerNumber + 1];
            foreach (var keyValue in rangeResponse.Kvs)
            {
                var broker = keyValue.Key.ToStringUtf8().Substring(BrokerTablePrefix.Length);
                var brokerNumberString = broker.Split('-').Last();
                int.TryParse(brokerNumberString, out var brokerNumber);
                var brokerSocket = new BrokerSocket();
                var connectionString = EnvironmentVariables.IsDev
                    ? "ws://localhost:5000/ws"
                    : $"ws://{broker}.broker.default.svc.cluster.local/ws";
                await brokerSocket.ConnectToBroker(connectionString);
                _brokerSockets[brokerNumber] = brokerSocket;
            }
        }

        public async Task CloseConnections()
        {
            foreach (var brokerSocket in _brokerSockets)
            {
                if (brokerSocket != null)
                    await brokerSocket.CloseConnection();
            }
            _client.Dispose();
        }

        public async Task Publish(MessageHeader header, Message message)
        {
            if (_batchingService.TryBatchMessage(header, message, out var queueFull))
            {
                if (queueFull != null)
                    await SendMessage(_serializer.Serialize(_batchingService.GetMessages(queueFull)), header);

                return;
            }

            var callback = new TimerCallback(async x =>
            {
                var messages = _batchingService.GetMessages(header);
                header.Print();
                await SendMessage(_serializer.Serialize(messages), header);
            });
            var timer = new Timer(callback, null, TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable), TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable));

            _batchingService.CreateBatch(header, message, timer);
        }

        private async Task SendMessage(byte[] message, MessageHeader header)
        {
            if(_brokerSocketsDict.TryGetValue($"{header.Topic}/{header.Partition}", out var brokerSocket))
                await brokerSocket.SendMessage(message);
            else 
                throw new Exception("Failed to get brokerSocket");
        }
    }
}
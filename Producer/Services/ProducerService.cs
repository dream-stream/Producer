using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using dotnet_etcd;
using Producer.Models.Messages;
using Producer.Serialization;
using Prometheus;

namespace Producer.Services
{
    public class ProducerService : IProducer
    {
        private readonly ISerializer _serializer;
        private readonly BatchingService _batchingService;
        private BrokerSocket[] _brokerSockets;
        private readonly Dictionary<string, BrokerSocket> _brokerSocketsDict = new Dictionary<string, BrokerSocket>();
        private EtcdClient _client;
        private int maxRetryCount = 15;

        private static readonly Counter MessagesBatched = Metrics.CreateCounter("messages_batched", "Number of messages added to batch.", new CounterConfiguration
        {
            LabelNames = new []{"TopicPartition"}
        });
        private static readonly Counter MessageBatchesSent = Metrics.CreateCounter("message_batches_sent", "Number of batches sent.", new CounterConfiguration
        {
            LabelNames = new[] { "BrokerConnection" }
        });

        private BrokerSocket _localhostBrokerSocket;


        public ProducerService(ISerializer serializer, BatchingService batchingService)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
        }

        public async Task InitSockets(EtcdClient client)
        {
            _client = client;
            _brokerSockets = await BrokerSocketHandler.UpdateBrokerSockets(client, _brokerSockets);
            await BrokerSocketHandler.UpdateBrokerSocketsDictionary(client, _brokerSocketsDict, _brokerSockets);
            client.WatchRange(BrokerSocketHandler.BrokerTablePrefix, async events => await BrokerSocketHandler.BrokerTableChangedHandler(events, _brokerSockets));
            client.WatchRange(BrokerSocketHandler.TopicTablePrefix, events => BrokerSocketHandler.TopicTableChangedHandler(events, _brokerSocketsDict, _brokerSockets));
        }

        public async Task InitSocketLocalhost()
        {
            _localhostBrokerSocket = new BrokerSocket();
            await _localhostBrokerSocket.ConnectToBroker("ws://localhost:5000/ws");
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
            MessagesBatched.WithLabels($"{header.Topic}/{header.Partition}").Inc();
            if (_batchingService.TryBatchMessage(header, message, out var queueFull))
            {
                if (queueFull == null) return;
                var messages = _batchingService.GetMessages(queueFull);
                await TryToSendWithRetries(header, messages);

                return;
            }

            var callback = new TimerCallback(async x =>
            {
                var messages = _batchingService.GetMessages(header);
                await TryToSendWithRetries(header, messages);
            });
            var timer = new Timer(callback, null, TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable), TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable));

            _batchingService.CreateBatch(header, message, timer);
        }

        private async Task TryToSendWithRetries(MessageHeader header, MessageContainer messages)
        {
            //var errorCount = 0;
            //while (true)
            //{
            //    try
            //    {
            //TODO For now just ignore messages failing to send.
            if (!await SendMessage(_serializer.Serialize<IMessage>(messages), header)) Console.WriteLine("Failed to send messages");
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine($"SendMessage retry {++errorCount}");
                //    Thread.Sleep(500*errorCount);
                //    if (errorCount != maxRetryCount) continue;
                //    Console.WriteLine($"Failed to send after {maxRetryCount} retries", e);
                //    throw;
                //}
            //}
        }

        private async Task<bool> SendMessage(byte[] message, MessageHeader header)
        {
            if (EnvironmentVariables.IsDev)
            {
                await _localhostBrokerSocket.SendMessage(message);
                return true;
            }

            if (_brokerSocketsDict.TryGetValue($"{header.Topic}/{header.Partition}", out var brokerSocket))
            {
                if(brokerSocket == null) throw new Exception("Failed to get brokerSocket");
                if(!brokerSocket.IsOpen()) return false;
                await brokerSocket.SendMessage(message);
                Console.WriteLine($"Sent batched messages to topic {header.Topic} with partition {header.Partition}");
                MessageBatchesSent.WithLabels(brokerSocket.ConnectedTo).Inc();
                MessageBatchesSent.WithLabels($"{header.Topic}/{header.Partition}").Inc();
                return true;
            }

            throw new Exception("Failed to get brokerSocket");
        }
    }
}
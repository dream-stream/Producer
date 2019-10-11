using System;
using System.Collections.Generic;
using System.Net.WebSockets;
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
        private BrokerSocket[] _brokerSockets;
        private readonly Dictionary<string, BrokerSocket> _brokerSocketsDict = new Dictionary<string, BrokerSocket>();
        private EtcdClient _client;

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
            var errorCount = 0;
            while (true)
            {
                try
                {
                    await SendMessage(_serializer.Serialize<IMessage>(messages), header);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"SendMessage retry {++errorCount}");
                    Thread.Sleep(1000);
                    if (errorCount != 5) continue;
                    Console.WriteLine("Failed to send after 5 retries", e);
                    throw;
                }
            }
        }

        private async Task SendMessage(byte[] message, MessageHeader header)
        {
            if (_brokerSocketsDict.TryGetValue($"{header.Topic}/{header.Partition}", out var brokerSocket))
            {
                if(brokerSocket == null) throw new Exception("Failed to get brokerSocket");
                await brokerSocket.SendMessage(message);
                Console.WriteLine($"Sent batched messages to topic {header.Topic} with partition {header.Partition}");
            }
            else 
                throw new Exception("Failed to get brokerSocket");
        }
    }
}
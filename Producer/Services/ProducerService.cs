using System;
using System.Threading;
using System.Threading.Tasks;
using Producer.Models.Messages;
using Producer.Serialization;

namespace Producer.Services
{
    public class ProducerService : IProducer
    {
        private readonly ISerializer _serializer;
        private readonly ProducerSocket _socket;
        private readonly BatchingService _batchingService;
        private readonly Semaphore _lock;

        public ProducerService(ISerializer serializer, ProducerSocket socket, BatchingService batchingService)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
            _lock = new Semaphore(1, 1);
        }

        public async Task Connect(string connectionString)
        {
            await _socket.ConnectToBroker(connectionString);
        }

        public async Task CloseConnection()
        {
            await _socket.CloseConnection();
        }

        public async Task Publish(MessageHeader header, Message message)
        {
            if (_batchingService.TryBatchMessage(header, message, out var queueKey))
            {
                if (queueKey != null)
                    await SendMessage(_serializer.Serialize<IMessage>(_batchingService.GetMessages(queueKey)));

                return;
            }

            var callback = new TimerCallback(async x =>
            {
                var messages = _batchingService.GetMessages(header);
                header.Print();
                await SendMessage(_serializer.Serialize<IMessage>(messages));
            });
            var timer = new Timer(callback, null, TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable), TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable));

            _batchingService.CreateBatch(header, message, timer);
        }

        private async Task SendMessage(byte[] message)
        {
            _lock.WaitOne();
            await _socket.SendMessage(message);
            _lock.Release();
        }
    }
}
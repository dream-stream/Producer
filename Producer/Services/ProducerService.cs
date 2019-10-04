using System;
using System.Threading;
using System.Threading.Tasks;
using Producer.Models.Messages;
using Producer.Serialization;

namespace Producer.Services
{
    public class ProducerService
    {
        private readonly ISerializer _serializer;
        private readonly ProducerSocket _socket;
        private readonly string _connectionString;
        private readonly BatchingService _batchingService;
        private readonly Semaphore _lock;

        public ProducerService(ISerializer serializer, ProducerSocket socket, BatchingService batchingService, string connectionString)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
            _lock = new Semaphore(1, 1);
            _connectionString = !string.IsNullOrEmpty(connectionString) ? connectionString : throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task ConnectToBroker()
        {
            await _socket.ConnectToBroker(_connectionString);
        }

        public async Task CloseConnection()
        {
            await _socket.CloseConnection();
        }

        public async Task AddMessage(MessageHeader header, Message message)
        {
            if (_batchingService.TryBatchMessage(header, message, out var queueKey))
            {
                if (queueKey != null)
                    await SendMessage(_serializer.Serialize(_batchingService.GetMessages(queueKey)));

                return;
            }

            var callback = new TimerCallback(async x =>
            {
                var messages = _batchingService.GetMessages(header);
                header.Print();
                await SendMessage(_serializer.Serialize(messages));
            });
            var timer = new Timer(callback, null, TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable), TimeSpan.FromSeconds(EnvironmentVariables.BatchTimerVariable));

            _batchingService.CreateBatch(header, message, timer);
        }

        public async Task SendMessage(byte[] message)
        {
            _lock.WaitOne();
            await _socket.SendMessage(message);
            _lock.Release();

        }
    }
}
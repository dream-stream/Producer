using System;
using System.Text.Json;
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

        public ProducerService(ISerializer serializer, ProducerSocket socket, BatchingService batchingService, string connectionString)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _batchingService = batchingService ?? throw new ArgumentNullException(nameof(batchingService));
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
            if (_batchingService.BatchMessage(header, message) == null)
                return;

            await SendMessage(header, _serializer.Serialize(_batchingService.GetMessages(header)));
        }

        public async Task SendMessage(MessageHeader header, byte[] message)
        {
            Console.WriteLine($"Sending Header: {JsonSerializer.Serialize(header)}:");
            await _socket.SendMessage(header.Serialize(_serializer));
            Console.WriteLine("Sending Message");
            await _socket.SendMessage(message);
        }
    }
}
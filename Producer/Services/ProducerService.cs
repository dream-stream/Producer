using System;
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

        public ProducerService(ISerializer serializer, ProducerSocket socket, string connectionString)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
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

        public async Task SendMessage(Message message)
        {
            var header = new MessageHeader
            {
                Topic = $"Topic {Environment.MachineName}", //TODO Calculate topic
                Partition = 3 //TODO Calculate the partition
            };

            Console.WriteLine("Sending Header");
            await _socket.SendMessage(header.Serialize(_serializer));
            Console.WriteLine("Sending Message");
            await _socket.SendMessage(message.Serialize(_serializer));
        }
    }
}
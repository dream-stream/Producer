using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Producer.Models.Messages;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService : BaseService
    {
        private readonly Guid _producerId;
        private readonly ClientWebSocket _socket;
        private readonly string _topic;
        private Message _message;
        private readonly ISerializer _serializer;


        public WebSocketService(ISerializer serializer, ClientWebSocket socket, string connectionAddress, string topic)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _topic = topic;

            _socket.ConnectAsync(new Uri(connectionAddress), CancellationToken.None).Wait();

            _producerId = Guid.NewGuid();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Graceful close", cancellationToken);
        }

        public override async void DoAsync(object state)
        {
            var messageHeader = new MessageHeader
            {
                ProducerId = _producerId,
                Topic = _topic,
                Partition = 3
            };
            var header = messageHeader.Serialize(_serializer);

            _message = new Message {Msg = GenerateMessages(1000).ToArray()};
            var message = _message.Serialize(_serializer);

            Console.WriteLine("Sending Header");
            await _socket.SendAsync(new ArraySegment<byte>(header, 0, header.Length), WebSocketMessageType.Binary, false, CancellationToken.None);
            Console.WriteLine("Sending Message");
            await _socket.SendAsync(new ArraySegment<byte>(message, 0, message.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        private static IEnumerable<string> GenerateMessages(int amount)
        {
            var list = new List<string>();

            for (var i = 0; i < amount; i++)
            {
                list.Add($"Hello World {i}");
            }

            return list;
        }
    }
}
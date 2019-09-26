using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using MessagePack;
using Producer.Models.Messages;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService : BaseService
    {
        private readonly Guid _producerId;
        private readonly ClientWebSocket _socket;
        private Message _message;
        private readonly ISerializer _serializer;

        public WebSocketService(ISerializer serializer, ClientWebSocket socket)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            
            _socket.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None).Wait();

            _producerId = Guid.NewGuid();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Graceful close", cancellationToken);
        }

        public override async Task DoAsync()
        {
            var messageHeader = new MessageHeader
            {
                ProducerId = _producerId,
                Topic = "TestTopic",
                Partition = 3
            };
            var header = LZ4MessagePackSerializer.Serialize<BaseMessage>(messageHeader);
            
            _message = new Message();
            _message.Msg = GenerateMessages(1000).ToArray();
            var message = LZ4MessagePackSerializer.Serialize<BaseMessage>(_message);

            while (true)
            {
                Console.WriteLine("Sending Header");
                await _socket.SendAsync(new ArraySegment<byte>(header, 0, header.Length), WebSocketMessageType.Binary, false, CancellationToken.None);
                Console.WriteLine("Sending Message");
                await _socket.SendAsync(new ArraySegment<byte>(message, 0, message.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
                
                await Task.Delay(1000 * 5);
            }
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
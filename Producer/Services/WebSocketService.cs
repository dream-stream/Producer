using System;
using System.Collections.Generic;
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
        private readonly byte[] _header;
        private readonly List<byte[]> _messages;
        private readonly ISerializer _serializer;

        public WebSocketService(ISerializer serializer, ClientWebSocket socket)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            
            _socket.ConnectAsync(new Uri("ws://localhost:5000/ws"), CancellationToken.None).Wait();

            _producerId = Guid.NewGuid();
            _messages = new List<byte[]>();
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
            var message = new Message
            {
                Msg = "Hello World!"
            };

            _messages.Add(messageHeader.Serialize(_serializer));
            _messages.Add(message.Serialize(_serializer));
            var counter = 0;

            while (true)
            {
                for (var i = 0; i < _messages.Count; i++)
                {
                    Console.WriteLine($"Sending message: {message.Msg}");
                    await _socket.SendAsync(new ArraySegment<byte>(_messages[i], 0, _messages[i].Length), WebSocketMessageType.Binary, ++counter == 6, CancellationToken.None);
                }

                await Task.Delay(1000 * 5);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Producer.Models;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService : BaseService
    {
        private ClientWebSocket _socket;
        private List<byte[]> _messages;
        private readonly ISerializer _serializer;

        public WebSocketService(ISerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //TODO Dependency injection
            _socket = new ClientWebSocket();
            _messages = new List<byte[]>();

            await _socket.ConnectAsync(new Uri("ws://localhost:5000/ws"), cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Graceful close", cancellationToken);
        }

        public override async Task DoAsync()
        {
            var msg = new PreProcessedMessage
            {
                Topic = "MyTopic",
                Partition = 1,
                Message = "Hello World"
            };

            var serializeMessage = _serializer.Serialize(msg.Message);

            while (true)
            {
                Console.WriteLine($"Sending message: {msg.Message}");
                await _socket.SendAsync(new ArraySegment<byte>(serializeMessage, 0, serializeMessage.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
                
                await Task.Delay(1000 * 5);
            }
        }

        private Task<byte[]> PrepareMessages(PreProcessedMessage message)
        {
            //TODO Serialize message
            //TODO Add messages to list
            //TODO Compress list

            //TODO Send list to Dream-Stream
            return new Task<byte[]>(() => new byte[12]);
        }
    }
}
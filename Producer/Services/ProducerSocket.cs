using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Producer.Services
{
    public class ProducerSocket
    {
        private readonly ClientWebSocket _clientWebSocket;

        public ProducerSocket()
        {
            _clientWebSocket = new ClientWebSocket();
        }

        public async Task ConnectToBroker(string connectionString)
        {
            await _clientWebSocket.ConnectAsync(new Uri(connectionString), CancellationToken.None);
        }

        public async Task SendMessage(byte[] message)
        {
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(message, 0, message.Length), WebSocketMessageType.Binary, false, CancellationToken.None);
        }

        public async Task CloseConnection()
        {
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
        }
    }
}

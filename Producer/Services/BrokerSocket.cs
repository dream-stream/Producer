using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Producer.Services
{
    public class BrokerSocket
    {
        private readonly ClientWebSocket _clientWebSocket;
        private readonly Semaphore _lock;

        public BrokerSocket()
        {
            _clientWebSocket = new ClientWebSocket();
            _lock = new Semaphore(1, 1);
        }

        public async Task ConnectToBroker(string connectionString)
        {
            await _clientWebSocket.ConnectAsync(new Uri(connectionString), CancellationToken.None);
        }

        public async Task SendMessage(byte[] message)
        {
            _lock.WaitOne();
            await _clientWebSocket.SendAsync(new ArraySegment<byte>(message, 0, message.Length), WebSocketMessageType.Binary, false, CancellationToken.None);
            _lock.Release();
        }

        public async Task CloseConnection()
        {
            await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal closure", CancellationToken.None);
        }
    }
}

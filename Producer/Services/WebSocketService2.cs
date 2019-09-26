using System.Net.WebSockets;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService2 : WebSocketService
    {
        public WebSocketService2(ISerializer serializer, ClientWebSocket socket) : base(serializer, socket, "ws://localhost:5000/ws", "Topic2")
        {
        }
    }
}
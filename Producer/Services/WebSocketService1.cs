using System.Net.WebSockets;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService1 : WebSocketService
    {
        public WebSocketService1(ISerializer serializer, ClientWebSocket socket) : base(serializer, socket, "ws://localhost:5000/ws", "Topic1")
        {
        }
    }
}
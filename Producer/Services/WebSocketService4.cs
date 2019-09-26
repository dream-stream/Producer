using System.Net.WebSockets;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService4 : WebSocketService
    {
        public WebSocketService4(ISerializer serializer, ClientWebSocket socket) : base(serializer, socket, "ws://broker-1.broker.default.svc.cluster.local/ws", "Topic4")
        {
        }
    }
}
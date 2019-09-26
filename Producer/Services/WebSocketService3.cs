using System.Net.WebSockets;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService3 : WebSocketService
    {
        public WebSocketService3(ISerializer serializer, ClientWebSocket socket) : base(serializer, socket, "ws://broker-0.broker.default.svc.cluster.local/ws", "Topic3")
        {
        }
    }
}
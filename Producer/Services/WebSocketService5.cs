using System.Net.WebSockets;
using Producer.Serialization;

namespace Producer.Services
{
    public class WebSocketService5 : WebSocketService
    {
        public WebSocketService5(ISerializer serializer, ClientWebSocket socket) : base(serializer, socket, "ws://broker-2.broker.default.svc.cluster.local/ws", "Topic5")
        {
        }
    }
}
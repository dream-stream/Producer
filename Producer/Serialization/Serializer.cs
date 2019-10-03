using MessagePack;
using Producer.Models.Messages;

namespace Producer.Serialization
{
    public class Serializer : ISerializer
    {
        public byte[] Serialize<T>(T obj) where T : BaseMessage
        {
            return LZ4MessagePackSerializer.Serialize<BaseMessage>(obj);
        }

        public T Deserialize<T>(byte[] message)
        {
            return LZ4MessagePackSerializer.Deserialize<T>(message);
        }
    }
}

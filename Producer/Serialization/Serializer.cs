using MessagePack;
using Producer.Models.Messages;

namespace Producer.Serialization
{
    public class Serializer : ISerializer
    {
        public byte[] Serialize<T>(T obj) where T : BaseTransferMessage
        {
            return LZ4MessagePackSerializer.Serialize(obj);
        }

        public T Deserialize<T>(byte[] message)
        {
            return LZ4MessagePackSerializer.Deserialize<T>(message);
        }
    }
}

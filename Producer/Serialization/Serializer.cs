using MessagePack;

namespace Producer.Serialization
{
    public class Serializer : ISerializer
    {
        public byte[] Serialize<T>(T obj)
        {
            return MessagePackSerializer.Serialize(obj);
        }

        public T Deserialize<T>(byte[] message)
        {
            return MessagePackSerializer.Deserialize<T>(message);
        }
    }
}

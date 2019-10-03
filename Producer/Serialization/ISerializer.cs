using Producer.Models.Messages;

namespace Producer.Serialization
{
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj) where T : BaseTransferMessage;
        T Deserialize<T>(byte[] message);
    }
}

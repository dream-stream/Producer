using System.Threading.Tasks;
using Producer.Models.Messages;

namespace Producer.Services
{
    public interface IProducer
    {
        Task Connect(string connectionString);
        Task Publish(MessageHeader header, Message message);
        Task CloseConnection();
    }
}

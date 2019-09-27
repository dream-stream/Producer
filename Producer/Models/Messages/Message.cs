using MessagePack;

namespace Producer.Models.Messages
{
    [MessagePackObject]
    public class Message : BaseMessage
    {
        [Key(1)]
        public string[] Msg { get; set; }

        //TODO Function to calculate Partition
    }
}
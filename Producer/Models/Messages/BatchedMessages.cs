using System.Collections.Generic;
using MessagePack;

namespace Producer.Models.Messages
{
    [MessagePackObject]
    public class BatchedMessages : BaseTransferMessage
    {
        [Key(1)]
        public List<Message> Messages { get; set; }
    }
}

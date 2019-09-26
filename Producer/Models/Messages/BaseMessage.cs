using System;
using MessagePack;
using Producer.Serialization;

namespace Producer.Models.Messages
{
    [Union(0, typeof(MessageHeader))]
    [Union(1, typeof(Message))]
    [MessagePackObject]
    public abstract class BaseMessage
    {
        public BaseMessage() { }

        public virtual byte[] Serialize(ISerializer serializer)
        {
            return serializer.Serialize(this);
        }
    }
}

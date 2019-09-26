﻿using System;
using MessagePack;

namespace Producer.Models.Messages
{
    [MessagePackObject]
    public class MessageHeader : BaseMessage
    {
        [Key(1)]
        public Guid ProducerId { get; set; }
        [Key(2)]
        public string Topic { get; set; }
        [Key(3)]
        public int Partition { get; set; }
    }
}

using System.Collections.Generic;
using Producer.Models.Messages;

namespace Producer.Services
{
    public class BatchingService
    {
        private readonly Dictionary<MessageHeader, BatchedMessages> _messages;
        private readonly int _batchingSize;

        public BatchingService(int batchingSize)
        {
            _batchingSize = batchingSize;
            _messages = new Dictionary<MessageHeader, BatchedMessages>();
        }

        public MessageHeader BatchMessage(MessageHeader header, Message message)
        {
            if (_messages.TryGetValue(header, out var list))
            {
                list.Messages.Add(message);
                return list.Messages.Count == _batchingSize ? header : null;
            }

            _messages.Add(header, new BatchedMessages {Messages = new List<Message> {message}});

            return null;
        }

        public BatchedMessages GetMessages(MessageHeader header)
        {
            if (!_messages.TryGetValue(header, out var list))
                throw new KeyNotFoundException(
                    $"Messages for {nameof(header.Topic)}: {header.Topic} and {nameof(header.Partition)}: {header.Partition} not found");
            
            var returnList = new BatchedMessages {Messages = new List<Message>(list.Messages)};
            list.Messages.Clear();
            return returnList;
        }

        //TODO Make Timed Functionality
    }
}

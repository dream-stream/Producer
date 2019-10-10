using System;
using System.Security.Cryptography;
using MessagePack;
using Producer.Models.Messages;
using Producer.Services;

namespace Producer
{
    public static class MessageGenerator
    {
        public static (MessageHeader[], Message[]) GenerateMessages(int amount, long partitions)
        {
            var messages = new Message[amount];
            var messageHeaders = new MessageHeader[amount];

            for (var i = 0; i < amount; i++) // TODO Generate more random messages
            {
                messages[i] = new Message
                {
                    Address = Addresses.GetAddress(),
                    LocationDescription = "First floor bathroom",
                    Measurement = 23.5,
                    SensorType = "Temperature",
                    Unit = "°C"
                };

                messageHeaders[i] = new MessageHeader{Topic = "SensorData", Partition = GetPartition(MessagePackSerializer.Serialize(messages[i].Address), partitions)};
            }

            return (messageHeaders, messages);
        }

        private static int GetPartition(byte[] message, long partitions)
        {
            using SHA512 sha512 = new SHA512Managed();
            var hash = sha512.ComputeHash(message);
            
            return (int)(Math.Abs(BitConverter.ToInt64(hash)) % partitions); //TODO Tryparse (Faster, better, stronger)
        }
    }
}
using System;

namespace Producer
{
    public static class EnvironmentVariables
    {
        public static string DevVariable { get; } = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        public static int AmountOfProducersVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("PRODUCER_AMOUNT") ?? "10");
        public static int AmountOfMessagesVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("MESSAGE_AMOUNT") ?? "1000");
        public static int BatchingSizeVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("BATCHING_SIZE") ?? "23");
        public static long PartitionAmountVariable { get; } = long.Parse(Environment.GetEnvironmentVariable("PARTITION_AMOUNT") ?? "5");
        public static int BatchTimerVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("BATCH_TIMER") ?? "10");
    }
}

using System;

namespace Producer
{
    public static class EnvironmentVariables
    {
        public static bool IsDev { get; } = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        public static int AmountOfMessagesVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("MESSAGE_AMOUNT") ?? "1000");
        public static int BatchingSizeVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("BATCHING_SIZE") ?? "23");
        public static long PartitionAmountVariable { get; } = long.Parse(Environment.GetEnvironmentVariable("PARTITION_AMOUNT") ?? "7");
        public static int BatchTimerVariable { get; } = int.Parse(Environment.GetEnvironmentVariable("BATCH_TIMER") ?? "10");
    }
}

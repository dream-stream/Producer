namespace Producer.Models
{
    public class PreProcessedMessage
    {
        public string Topic { get; set; }
        public int Partition { get; set; }
        public string Message { get; set; }
    }
}

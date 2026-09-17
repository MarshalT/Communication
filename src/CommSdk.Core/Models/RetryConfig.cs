namespace CommSdk.Core.Models
{
    public class RetryConfig
    {
        public int Count { get; set; }
        public int TimeoutMs { get; set; }
        public int DelayMs { get; set; }
        public bool ExponentialBackoff { get; set; }
    }
}

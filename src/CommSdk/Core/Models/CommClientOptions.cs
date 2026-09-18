namespace CommSdk.Core.Models
{
    public class CommClientOptions
    {
        public CommClientOptions()
        {
            RetryDelayMs = 100;
            AutoOpen = true;
            CloseOnDispose = true;
        }

        public int RetryCount { get; set; }
        public int RetryDelayMs { get; set; }
        public bool ExponentialBackoff { get; set; }
        public int TimeoutMs { get; set; }
        public bool AutoOpen { get; set; }
        public bool CloseOnDispose { get; set; }
    }
}

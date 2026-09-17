using System.Collections.Generic;

namespace CommSdk.Core.Models
{
    public class DeviceProfile
    {
        public string Id { get; set; }
        public string Model { get; set; }
        public string Driver { get; set; }
        public TransportConfig Transport { get; set; }
        public ProtocolConfig Protocol { get; set; }
        public RetryConfig Retry { get; set; }
        public LoggingConfig Logging { get; set; }
        public IDictionary<string, string> Custom { get; set; }
    }
}

using System.Collections.Generic;

namespace CommSdk.Core.Models
{
    public class TransportConfig
    {
        public string Type { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}

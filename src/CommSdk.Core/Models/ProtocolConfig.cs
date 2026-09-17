using System.Collections.Generic;

namespace CommSdk.Core.Models
{
    public class ProtocolConfig
    {
        public string Type { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}

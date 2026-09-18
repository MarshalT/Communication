using System.Collections.Generic;

namespace CommSdk.Core.Models
{
    public class DeviceCommand
    {
        public string Name { get; set; }
        public IDictionary<string, string> Parameters { get; set; }
    }
}

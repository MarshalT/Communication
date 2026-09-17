using CommSdk.Core.Abstractions;
using CommSdk.Core.Managers;

namespace CommSdk.Core.Models
{
    public class DeviceContext
    {
        public DeviceProfile Profile { get; set; }
        public ITransport Transport { get; set; }
        public IProtocol Protocol { get; set; }
        public CommClient Client { get; set; }
    }
}

using CommSdk.Core.Models;

namespace CommSdk.Core.Abstractions
{
    public interface IDeviceDriverFactory
    {
        string Model { get; }
        IDeviceDriver Create(DeviceProfile profile);
    }
}

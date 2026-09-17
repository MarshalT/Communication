using CommSdk.Core.Models;

namespace CommSdk.Core.Abstractions
{
    public interface IDeviceDriver
    {
        string Model { get; }
        void Initialize(DeviceContext context);
        object Read(DeviceCommand command);
        void Write(DeviceCommand command, object value);
        object Execute(DeviceCommand command);
    }
}

using System;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;

namespace CommSdk.Devices.Drivers
{
    public class ReflectionDeviceDriverFactory : IDeviceDriverFactory
    {
        private readonly Type _driverType;

        public ReflectionDeviceDriverFactory(string model, Type driverType)
        {
            if (string.IsNullOrEmpty(model)) throw new ArgumentException("Driver model is required", "model");
            if (driverType == null) throw new ArgumentNullException("driverType");
            if (!typeof(IDeviceDriver).IsAssignableFrom(driverType))
                throw new ArgumentException("Driver type must implement IDeviceDriver", "driverType");
            Model = model;
            _driverType = driverType;
        }

        public string Model { get; private set; }

        public IDeviceDriver Create(DeviceProfile profile)
        {
            return (IDeviceDriver)Activator.CreateInstance(_driverType);
        }
    }
}

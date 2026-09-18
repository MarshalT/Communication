using System;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;

namespace CommSdk.Devices.Drivers
{
    public abstract class DeviceDriverBase : IDeviceDriver
    {
        protected DeviceContext Context { get; private set; }
        public abstract string Model { get; }

        public virtual void Initialize(DeviceContext context)
        {
            Context = context ?? throw new ArgumentNullException("context");
        }

        public abstract object Read(DeviceCommand command);
        public abstract void Write(DeviceCommand command, object value);
        public abstract object Execute(DeviceCommand command);
    }
}

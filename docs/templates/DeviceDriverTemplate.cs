using System;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;
using CommSdk.Devices.Drivers;

public sealed class ExampleDriver : DeviceDriverBase
{
    public override string Model { get { return "example-device"; } }

    public override object Read(DeviceCommand command)
    {
        throw new DeviceException("Implement device-specific read commands here");
    }

    public override void Write(DeviceCommand command, object value)
    {
        throw new DeviceException("Implement device-specific write commands here");
    }

    public override object Execute(DeviceCommand command)
    {
        throw new DeviceException("Implement device-specific execute commands here");
    }
}

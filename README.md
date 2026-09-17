# CommSdk

CommSdk is a layered C# communication SDK targeting .NET Framework 4.8. It provides transport abstractions, serial/TCP/UDP transports, Modbus RTU/TCP/ASCII protocols, device-driver registration, JSON profiles, and synchronous/asynchronous request APIs.

## Projects

- `CommSdk.Core`: contracts, profiles, registries, client, session lifecycle, and logging.
- `CommSdk.Transports`: serial, TCP, and UDP implementations.
- `CommSdk.Protocols.Modbus`: Modbus framing, CRC/LRC, validation, and factories.
- `CommSdk.Devices`: driver base class, reflection factory, and assembly loader.
- `CommSdk.Samples`: registration and driver usage example.
- `tests/CommSdk.Tests`: protocol, profile, framing, and retry tests.

## Build and test

Open `CommSdk.sln` in Visual Studio with .NET Framework 4.8 targeting support, or run:

```text
dotnet build CommSdk.sln --configuration Release
dotnet test CommSdk.sln
```

The target framework is `net48`; building the SDK on macOS requires a Windows/.NET Framework-compatible build environment for the final verification.

## Register and use a device

```csharp
var profile = DeviceProfileJsonLoader.Load(File.ReadAllText("device-profile.json"));
var transports = new TransportRegistry();
transports.Register(new TcpTransportFactory());

var protocols = new ProtocolRegistry();
protocols.Register(new ModbusTcpProtocolFactory());

var drivers = new DriverRegistry();
drivers.Register(new MyDriverFactory());

using (var manager = new DeviceManager(transports, protocols, drivers))
using (var session = manager.Create(profile))
{
    session.Open();
    var result = session.Read(new DeviceCommand
    {
        Name = "readHoldingRegisters",
        Parameters = new Dictionary<string, string>
        {
            { "address", "0" },
            { "quantity", "2" }
        }
    });
}
```

`CommClient` serializes request/response operations, assembles protocol frames from transport chunks, validates Modbus transaction/slave/function identity, and retries transport failures according to `profile.retry`.

## Extension points

Implement `ITransportFactory`, `IProtocolFactory`, or `IDeviceDriverFactory` and register the factory. A third-party driver assembly containing parameterless `IDeviceDriver` implementations can be loaded with `DeviceDriverAssemblyLoader.RegisterAll`.

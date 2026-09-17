# CommSdk

CommSdk is a layered C# communication SDK targeting .NET Framework 4.8. It provides transport abstractions, serial/TCP/UDP transports, Modbus RTU/TCP/ASCII protocols, device-driver registration, JSON profiles, and synchronous/asynchronous request APIs.

## Projects

- `CommSdk.Core`: contracts, profiles, registries, client, session lifecycle, and logging.
- `CommSdk.Transports`: serial, TCP, and UDP implementations.
- `CommSdk.Protocols.Modbus`: Modbus framing, CRC/LRC, validation, and factories.
- `CommSdk.Devices`: generic driver base class, reflection factory, and assembly loader.
- `CommSdk.Samples`: executable usage example.
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

如果只使用本示例的串口电表客户端，不需要在业务代码中手动注册组件。`ElectricityMeterClient` 已经封装了串口、Modbus RTU 和连接生命周期：

```csharp
using (var meter = ElectricityMeterClient.FromJsonFile("docs/examples/electricity-meter-profile.json"))
{
    meter.Open();
    var energy = meter.ReadEnergy();
    Console.WriteLine("累计电量: " + energy + " kWh");
}
```

只有在替换传输方式、协议或接入其他设备驱动时，才需要使用底层 `TransportRegistry`、`ProtocolRegistry` 和 `DriverRegistry`，这样 SDK 才能继续支持第三方扩展。

## Serial Modbus electricity meter example

`CommSdk.Samples` contains a complete serial Modbus RTU example for reading cumulative energy. The client sends a read-holding-registers request (`0x03`) and prints the decoded value in `kWh`:

```text
dotnet run --project src/CommSdk.Samples -- docs/examples/electricity-meter-profile.json
```

The profile uses `COM3`, 9600 8N1, slave address `1`, register address `0`, two registers, `UInt32BE`, and a `0.01` scale by default. Change the string values under `device.custom` in [electricity-meter-profile.json](/Users/tangjianhong/脚本/Communication/docs/examples/electricity-meter-profile.json) to match the meter manual. Modbus register addresses in this example are zero-based; some vendor manuals display one-based addresses such as `40001`, which must be converted before configuring `energyAddress`.

JSON 文件标准格式不支持注释，因此配置字段说明写在这里：`energyAddress` 是电量寄存器零基地址，`energyQuantity` 是读取的寄存器数量，`energyScale` 是原始值到 kWh 的倍率，`energyByteOrder` 是 32 位数值的字节序。串口参数位于 `transport`，Modbus 从站地址位于 `protocol.station`。

`ReadEnergy` returns a `double` in kWh. `energyFunction` may be set to `0x04` for input registers, and `energyByteOrder` supports `UInt32BE`, `UInt32LE`, `UInt32WordSwapBE`, and `UInt32WordSwapLE`. A call can override profile values when a device exposes more than one energy register:

```csharp
var energy = meter.ReadEnergy();
Console.WriteLine("累计电量: " + energy + " kWh");
```

The sample requires a real serial adapter and a meter that responds to the configured register map. If the meter uses a 64-bit value, IEEE-754 float, signed value, or a vendor-specific scaling rule, extend the driver decoder to match that device's register table.

## Extension points

Implement `ITransportFactory`, `IProtocolFactory`, or `IDeviceDriverFactory` and register the factory. A third-party driver assembly containing parameterless `IDeviceDriver` implementations can be loaded with `DeviceDriverAssemblyLoader.RegisterAll`.

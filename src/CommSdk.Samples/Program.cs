using System;
using System.Collections.Generic;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Managers;
using CommSdk.Core.Models;
using CommSdk.Core.Registries;
using CommSdk.Core.Logging;
using CommSdk.Devices.Drivers;
using CommSdk.Protocols.Modbus.Factories;
using CommSdk.Protocols.Modbus.Models;
using CommSdk.Transports.Factories;

namespace CommSdk.Samples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            LogManager.Current = new ConsoleLog();

            var transportRegistry = new TransportRegistry();
            transportRegistry.Register(new SerialTransportFactory());
            transportRegistry.Register(new TcpTransportFactory());
            transportRegistry.Register(new UdpTransportFactory());

            var protocolRegistry = new ProtocolRegistry();
            protocolRegistry.Register(new ModbusRtuProtocolFactory());
            protocolRegistry.Register(new ModbusTcpProtocolFactory());
            protocolRegistry.Register(new ModbusAsciiProtocolFactory());

            var driverRegistry = new DriverRegistry();
            driverRegistry.Register(new SampleDriverFactory());

            var manager = new DeviceManager(transportRegistry, protocolRegistry, driverRegistry);

            var profile = new DeviceProfile
            {
                Id = "device-1",
                Model = "sample-device",
                Driver = "sample-driver",
                Transport = new TransportConfig
                {
                    Type = "tcp",
                    Parameters = new Dictionary<string, string>
                    {
                        {"host", "127.0.0.1"},
                        {"port", "502"}
                    }
                },
                Protocol = new ProtocolConfig
                {
                    Type = "modbus-tcp",
                    Parameters = new Dictionary<string, string>()
                }
            };

            using (var session = manager.Create(profile))
            {
                Console.WriteLine("Session created for model: " + session.Profile.Model);
                Console.WriteLine("Transport: " + session.Transport.Name + ", protocol: " + session.Protocol.Name);
                Console.WriteLine("Call session.Open() only when a Modbus TCP endpoint is available.");
            }
        }
    }

    internal class SampleDriverFactory : IDeviceDriverFactory
    {
        public string Model { get { return "sample-device"; } }
        public IDeviceDriver Create(DeviceProfile profile)
        {
            return new SampleDriver();
        }
    }

    internal class SampleDriver : DeviceDriverBase
    {
        public override string Model { get { return "sample-device"; } }

        public override object Read(DeviceCommand command)
        {
            if (command == null || !string.Equals(command.Name, "readHoldingRegisters", StringComparison.OrdinalIgnoreCase))
                throw new DeviceException("Unsupported sample read command");

            var address = ParseUShort(command, "address");
            var quantity = ParseUShort(command, "quantity");
            return Context.Client.Send(new ModbusRequest
            {
                TransactionId = 1,
                SlaveId = 1,
                FunctionCode = 0x03,
                Address = address,
                Quantity = quantity
            });
        }

        public override void Write(DeviceCommand command, object value)
        {
            throw new DeviceException("Sample driver does not implement write commands");
        }

        public override object Execute(DeviceCommand command)
        {
            throw new DeviceException("Sample driver does not implement execute commands");
        }

        private static ushort ParseUShort(DeviceCommand command, string key)
        {
            string value;
            if (command.Parameters == null || !command.Parameters.TryGetValue(key, out value))
                throw new DeviceException("Missing command parameter: " + key);
            ushort parsed;
            if (!ushort.TryParse(value, out parsed))
                throw new DeviceException("Invalid command parameter: " + key);
            return parsed;
        }
    }
}

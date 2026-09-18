using System;
using CommSdk.Core.Configuration;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;
using CommSdk.Framework;
using Xunit;

namespace CommSdk.Tests
{
    public class ConfigurationTests
    {
        [Fact]
        public void LoadsDesignProfileShape()
        {
            var profile = DeviceProfileJsonLoader.Load(@"{
              ""device"": { ""id"": ""d1"", ""model"": ""INV-100"", ""driver"": ""InverterDriver"" },
              ""transport"": { ""type"": ""serial"", ""port"": ""COM3"", ""baud"": 9600 },
              ""protocol"": { ""type"": ""modbus-rtu"", ""station"": 1, ""endianness"": ""LE"" },
              ""retry"": { ""count"": 3, ""timeoutMs"": 1000 },
              ""logging"": { ""level"": ""INFO"" }
            }");

            Assert.Equal("INV-100", profile.Model);
            Assert.Equal("COM3", profile.Transport.Parameters["port"]);
            Assert.Equal("9600", profile.Transport.Parameters["baud"]);
            Assert.Equal("1", profile.Protocol.Parameters["station"]);
            Assert.NotNull(profile.Custom);
            Assert.Equal(3, profile.Retry.Count);
            Assert.Equal("INFO", profile.Logging.Level);
        }

        [Fact]
        public void LoadsDeviceCustomParameters()
        {
            var profile = DeviceProfileJsonLoader.Load(@"{
              ""device"": {
                ""model"": ""electricity-meter"",
                ""custom"": { ""energyAddress"": ""256"", ""energyScale"": ""0.1"" }
              },
              ""transport"": { ""type"": ""serial"", ""port"": ""COM3"" },
              ""protocol"": { ""type"": ""modbus-rtu"", ""station"": 1 }
            }");

            Assert.Equal("256", profile.Custom["energyAddress"]);
            Assert.Equal("0.1", profile.Custom["energyScale"]);
        }

        [Fact]
        public void ResolvesTcpModbusCommunicationMode()
        {
            var mode = CommunicationProfileResolver.Resolve(new DeviceProfile
            {
                Transport = new TransportConfig { Type = "TCP" },
                Protocol = new ProtocolConfig { Type = "MODBUS-TCP" }
            });

            Assert.Equal(CommunicationMode.ModbusTcp, mode);
        }

        [Fact]
        public void ResolvesSerialRtuCommunicationMode()
        {
            var mode = CommunicationProfileResolver.Resolve(new DeviceProfile
            {
                Transport = new TransportConfig { Type = "serial" },
                Protocol = new ProtocolConfig { Type = "modbus-rtu" }
            });

            Assert.Equal(CommunicationMode.ModbusRtu, mode);
        }

        [Fact]
        public void RejectsUnsupportedCommunicationCombination()
        {
            Assert.Throws<DeviceException>(() => CommunicationProfileResolver.Resolve(new DeviceProfile
            {
                Transport = new TransportConfig { Type = "udp" },
                Protocol = new ProtocolConfig { Type = "modbus-tcp" }
            }));
        }

        [Fact]
        public void RejectsMissingCommunicationConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() => CommunicationProfileResolver.Resolve(null));
            Assert.Throws<DeviceException>(() => CommunicationProfileResolver.Resolve(new DeviceProfile()));
        }

        [Fact]
        public void CreatesClientDirectlyFromTcpProfile()
        {
            using (var client = CommClientFactory.Create(new DeviceProfile
            {
                Transport = new TransportConfig { Type = "tcp" },
                Protocol = new ProtocolConfig { Type = "modbus-tcp" }
            }))
            {
                Assert.Equal("tcp", client.Transport.Name);
                Assert.Equal("modbus-tcp", client.Protocol.Name);
            }
        }

        [Fact]
        public void CreatesClientDirectlyFromSerialProfile()
        {
            using (var client = CommClientFactory.Create(new DeviceProfile
            {
                Transport = new TransportConfig { Type = "serial" },
                Protocol = new ProtocolConfig { Type = "modbus-rtu" }
            }))
            {
                Assert.Equal("serial", client.Transport.Name);
                Assert.Equal("modbus-rtu", client.Protocol.Name);
            }
        }
    }
}

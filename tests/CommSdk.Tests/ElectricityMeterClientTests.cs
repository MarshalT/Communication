using CommSdk.Core.Exceptions;
using CommSdk.Samples;
using Xunit;

namespace CommSdk.Tests
{
    public class ElectricityMeterClientTests
    {
        [Fact]
        public void CreatesClosedClientFromSerialRtuProfile()
        {
            using (var client = ElectricityMeterClient.FromJson(@"{
              ""device"": { ""model"": ""electricity-meter"" },
              ""transport"": { ""type"": ""serial"", ""port"": ""COM3"", ""baud"": 9600 },
              ""protocol"": { ""type"": ""modbus-rtu"", ""station"": 1 }
            }") )
            {
                Assert.False(client.IsOpen);
            }
        }

        [Fact]
        public void RejectsNonSerialTransport()
        {
            Assert.Throws<DeviceException>(() => ElectricityMeterClient.FromJson(@"{
              ""device"": { ""model"": ""electricity-meter"" },
              ""transport"": { ""type"": ""tcp"", ""host"": ""127.0.0.1"", ""port"": ""502"" },
              ""protocol"": { ""type"": ""modbus-tcp"", ""station"": 1 }
            }") );
        }
    }
}

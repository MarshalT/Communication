using CommSdk.Core.Configuration;
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
            Assert.Equal(3, profile.Retry.Count);
            Assert.Equal("INFO", profile.Logging.Level);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;
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
        public void RejectsUnsupportedTransportProtocolCombination()
        {
            Assert.Throws<DeviceException>(() => ElectricityMeterClient.FromJson(@"{
              ""device"": { ""model"": ""electricity-meter"" },
              ""transport"": { ""type"": ""udp"", ""host"": ""127.0.0.1"", ""port"": ""502"" },
              ""protocol"": { ""type"": ""modbus-tcp"", ""station"": 1 }
            }") );
        }

        [Fact]
        public async Task ReadsEnergyOverModbusTcp()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var serverTask = Task.Run(async () =>
            {
                using (var server = await listener.AcceptTcpClientAsync().ConfigureAwait(false))
                {
                    var stream = server.GetStream();
                    var request = await ReadExact(stream, 12).ConfigureAwait(false);
                    Assert.Equal((byte)0, request[2]);
                    Assert.Equal((byte)0, request[3]);
                    Assert.Equal((byte)1, request[6]);
                    Assert.Equal((byte)3, request[7]);

                    // 返回 0x00003039，按 0.01 倍率换算为 123.45 kWh。
                    var response = new byte[]
                    {
                        request[0], request[1], 0x00, 0x00, 0x00, 0x07, 0x01,
                        0x03, 0x04, 0x00, 0x00, 0x30, 0x39
                    };
                    await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                }
            });

            try
            {
                using (var client = ElectricityMeterClient.Create(new DeviceProfile
                {
                    Model = "electricity-meter",
                    Transport = new TransportConfig
                    {
                        Type = "tcp",
                        Parameters = new Dictionary<string, string>
                        {
                            { "host", "127.0.0.1" },
                            { "port", port.ToString() },
                            { "connectTimeoutMs", "2000" },
                            { "readTimeoutMs", "2000" },
                            { "writeTimeoutMs", "2000" }
                        }
                    },
                    Protocol = new ProtocolConfig
                    {
                        Type = "modbus-tcp",
                        Parameters = new Dictionary<string, string> { { "station", "1" } }
                    },
                    Custom = new Dictionary<string, string>
                    {
                        { "energyAddress", "0" },
                        { "energyQuantity", "2" },
                        { "energyScale", "0.01" },
                        { "energyByteOrder", "UInt32BE" }
                    }
                }))
                {
                    client.Open();
                    Assert.Equal(123.45d, client.ReadEnergy(), 2);
                }

                await serverTask.ConfigureAwait(false);
            }
            finally
            {
                listener.Stop();
            }
        }

        private static async Task<byte[]> ReadExact(NetworkStream stream, int length)
        {
            var buffer = new byte[length];
            var offset = 0;
            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer, offset, length - offset).ConfigureAwait(false);
                if (read == 0) throw new InvalidOperationException("TCP server received an incomplete request");
                offset += read;
            }
            return buffer;
        }
    }
}

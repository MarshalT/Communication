using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommSdk.Protocols.Modbus;
using CommSdk.Protocols.Modbus.Models;
using CommSdk.Transports;
using CommSdk.Transports.Options;
using Xunit;

namespace CommSdk.Tests
{
    public class TcpLoopbackTests
    {
        [Fact]
        public async Task TcpTransportCompletesModbusRoundTrip()
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
                    Assert.Equal((byte)0x42, request[1]);
                    var response = new byte[]
                    {
                        0x00, 0x42, 0x00, 0x00, 0x00, 0x07, 0x01,
                        0x03, 0x04, 0x00, 0x01, 0x00, 0x02
                    };
                    await stream.WriteAsync(response, 0, 5).ConfigureAwait(false);
                    await Task.Delay(5).ConfigureAwait(false);
                    await stream.WriteAsync(response, 5, response.Length - 5).ConfigureAwait(false);
                }
            });

            try
            {
                var transport = new TcpTransport(new TcpTransportOptions
                {
                    Host = "127.0.0.1",
                    Port = port,
                    ConnectTimeoutMs = 2000,
                    ReadTimeoutMs = 2000,
                    WriteTimeoutMs = 2000,
                    BufferSize = 64
                });
                using (var client = new CommSdk.Core.Managers.CommClient(transport, new ModbusTcpProtocol()))
                {
                    var response = (ModbusResponse)await client.SendAsync(new ModbusRequest
                    {
                        TransactionId = 0x42,
                        SlaveId = 1,
                        FunctionCode = 3,
                        Address = 0,
                        Quantity = 2
                    }).ConfigureAwait(false);
                    Assert.Equal(4, response.Data.Length);
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
                if (read == 0) throw new InvalidOperationException("Loopback server received an incomplete request");
                offset += read;
            }
            return buffer;
        }
    }
}

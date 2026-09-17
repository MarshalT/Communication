using CommSdk.Core.Managers;
using CommSdk.Core.Models;
using CommSdk.Protocols.Modbus;
using CommSdk.Protocols.Modbus.Models;
using Xunit;

namespace CommSdk.Tests
{
    public class CommClientTests
    {
        [Fact]
        public void AssemblesTcpResponseAcrossTransportChunks()
        {
            var transport = new FakeTransport();
            transport.Enqueue(
                new byte[] { 0x00, 0x42, 0x00 },
                new byte[] { 0x00, 0x00, 0x07, 0x01, 0x03, 0x04, 0x00, 0x01, 0x00, 0x02 });
            var request = new ModbusRequest
            {
                TransactionId = 0x42,
                SlaveId = 1,
                FunctionCode = 3,
                Quantity = 2
            };

            using (var client = new CommClient(transport, new ModbusTcpProtocol()))
            {
                var response = (ModbusResponse)client.Send(request);
                Assert.Equal((ushort)0x42, response.TransactionId);
                Assert.Equal(4, response.Data.Length);
                Assert.Single(transport.Sent);
            }
        }

        [Fact]
        public void RetriesTransportFailureAndReconnects()
        {
            var transport = new FakeTransport { FailSendCount = 1 };
            transport.Enqueue(new byte[] { 0x00, 0x42, 0x00, 0x00, 0x00, 0x07, 0x01, 0x03, 0x04, 0x00, 0x01, 0x00, 0x02 });
            var options = new CommClientOptions { RetryCount = 1, RetryDelayMs = 1 };
            var request = new ModbusRequest
            {
                TransactionId = 0x42,
                SlaveId = 1,
                FunctionCode = 3,
                Quantity = 2
            };

            using (var client = new CommClient(transport, new ModbusTcpProtocol(), options))
            {
                var response = (ModbusResponse)client.Send(request);
                Assert.False(response.IsException);
                Assert.True(transport.OpenCount >= 2);
                Assert.True(transport.CloseCount >= 1);
            }
        }
    }
}

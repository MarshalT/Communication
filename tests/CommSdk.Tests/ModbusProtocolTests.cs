using System;
using CommSdk.Core.Exceptions;
using CommSdk.Protocols.Modbus;
using CommSdk.Protocols.Modbus.Models;
using Xunit;

namespace CommSdk.Tests
{
    public class ModbusProtocolTests
    {
        [Fact]
        public void RtuEncodeProducesKnownCrcFrame()
        {
            var frame = new ModbusRtuProtocol().Encode(new ModbusRequest
            {
                SlaveId = 1,
                FunctionCode = 3,
                Address = 0,
                Quantity = 2
            });

            Assert.Equal(new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x02, 0xC4, 0x0B }, frame);
        }

        [Fact]
        public void RtuDecodeRejectsBadCrc()
        {
            var frame = new byte[] { 0x01, 0x03, 0x02, 0x00, 0x01, 0x00 };
            Assert.Throws<ProtocolException>(() => new ModbusRtuProtocol().Decode(frame));
        }

        [Fact]
        public void TcpEncodeProducesExpectedMbapFrame()
        {
            var frame = new ModbusTcpProtocol().Encode(new ModbusRequest
            {
                TransactionId = 0x42,
                SlaveId = 1,
                FunctionCode = 3,
                Address = 0,
                Quantity = 2
            });

            Assert.Equal(
                new byte[] { 0x00, 0x42, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x02 },
                frame);
        }

        [Fact]
        public void TcpDecodeRejectsInvalidLength()
        {
            var frame = new byte[] { 0x00, 0x42, 0x00, 0x00, 0x00, 0x00, 0x01 };
            Assert.Throws<ProtocolException>(() => new ModbusTcpProtocol().Decode(frame));
        }

        [Fact]
        public void AsciiEncodeUsesLrcAndRoundTrips()
        {
            var protocol = new ModbusAsciiProtocol();
            var requestFrame = protocol.Encode(new ModbusRequest
            {
                SlaveId = 1,
                FunctionCode = 3,
                Address = 0,
                Quantity = 2
            });

            Assert.Equal(":010300000002FA\r\n", System.Text.Encoding.ASCII.GetString(requestFrame));
            var response = (ModbusResponse)protocol.Decode(
                System.Text.Encoding.ASCII.GetBytes(":01030400010002F5\r\n"));
            Assert.Equal((byte)1, response.SlaveId);
            Assert.Equal((byte)3, response.FunctionCode);
            Assert.Equal(4, response.Data.Length);
        }
    }
}

using System;
using System.Text;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;
using CommSdk.Protocols.Modbus.Models;
using CommSdk.Protocols.Modbus.Utils;

namespace CommSdk.Protocols.Modbus
{
    public class ModbusAsciiProtocol : IProtocol, IProtocolFrameParser, IProtocolResponseValidator
    {
        public string Name { get { return "modbus-ascii"; } }

        public byte[] Encode(IProtocolRequest request)
        {
            var modbusRequest = request as ModbusRequest;
            if (modbusRequest == null) throw new ProtocolException("Invalid request type");

            var pdu = ModbusCodec.BuildRequestPdu(modbusRequest);
            var body = new byte[1 + pdu.Length + 1];
            body[0] = modbusRequest.SlaveId;
            Buffer.BlockCopy(pdu, 0, body, 1, pdu.Length);
            body[body.Length - 1] = ModbusLrc.Compute(body, 0, body.Length - 1);
            return Encoding.ASCII.GetBytes(":" + HexUtils.ToHex(body) + "\r\n");
        }

        public IProtocolResponse Decode(byte[] frame)
        {
            if (frame == null || frame.Length < 9) throw new ProtocolException("Invalid ASCII frame");

            var text = Encoding.ASCII.GetString(frame);
            if (!text.StartsWith(":")) throw new ProtocolException("ASCII frame must start with ':'");
            if (!text.EndsWith("\r\n")) throw new ProtocolException("ASCII frame must end with CRLF");

            var hex = text.Substring(1, text.Length - 3);
            var body = HexUtils.FromHex(hex);
            if (body.Length < 3) throw new ProtocolException("ASCII frame too short");

            var lrcExpected = body[body.Length - 1];
            var lrcActual = ModbusLrc.Compute(body, 0, body.Length - 1);
            if (lrcExpected != lrcActual) throw new ProtocolException("LRC check failed");

            var slaveId = body[0];
            var pduLength = body.Length - 2;
            var pdu = new byte[pduLength];
            Buffer.BlockCopy(body, 1, pdu, 0, pduLength);
            return ModbusCodec.ParseResponse(slaveId, pdu, 0);
        }

        public bool TryGetFrameLength(byte[] buffer, int offset, int count, out int frameLength)
        {
            return ModbusFrameParser.TryGetAsciiFrameLength(buffer, offset, count, out frameLength);
        }

        public void ValidateResponse(IProtocolRequest request, IProtocolResponse response)
        {
            ModbusResponseValidator.Validate(request as ModbusRequest, response as ModbusResponse, false);
        }
    }
}

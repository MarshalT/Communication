using System;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;
using CommSdk.Protocols.Modbus.Models;
using CommSdk.Protocols.Modbus.Utils;

namespace CommSdk.Protocols.Modbus
{
    public class ModbusRtuProtocol : IProtocol, IProtocolFrameParser, IProtocolResponseValidator
    {
        public string Name { get { return "modbus-rtu"; } }

        public byte[] Encode(IProtocolRequest request)
        {
            var modbusRequest = request as ModbusRequest;
            if (modbusRequest == null) throw new ProtocolException("Invalid request type");

            var pdu = ModbusCodec.BuildRequestPdu(modbusRequest);
            var frame = new byte[1 + pdu.Length + 2];
            frame[0] = modbusRequest.SlaveId;
            Buffer.BlockCopy(pdu, 0, frame, 1, pdu.Length);

            var crc = ModbusCrc16.Compute(frame, 0, frame.Length - 2);
            frame[frame.Length - 2] = (byte)(crc & 0xFF);
            frame[frame.Length - 1] = (byte)(crc >> 8);
            return frame;
        }

        public IProtocolResponse Decode(byte[] frame)
        {
            if (frame == null || frame.Length < 5) throw new ProtocolException("Invalid RTU frame");

            var crcExpected = (ushort)(frame[frame.Length - 2] | (frame[frame.Length - 1] << 8));
            var crcActual = ModbusCrc16.Compute(frame, 0, frame.Length - 2);
            if (crcExpected != crcActual) throw new ProtocolException("CRC check failed");

            var slaveId = frame[0];
            var pduLength = frame.Length - 3;
            var pdu = new byte[pduLength];
            Buffer.BlockCopy(frame, 1, pdu, 0, pduLength);
            return ModbusCodec.ParseResponse(slaveId, pdu, 0);
        }

        public bool TryGetFrameLength(byte[] buffer, int offset, int count, out int frameLength)
        {
            return ModbusFrameParser.TryGetRtuFrameLength(buffer, offset, count, out frameLength);
        }

        public void ValidateResponse(IProtocolRequest request, IProtocolResponse response)
        {
            ModbusResponseValidator.Validate(request as ModbusRequest, response as ModbusResponse, false);
        }
    }
}

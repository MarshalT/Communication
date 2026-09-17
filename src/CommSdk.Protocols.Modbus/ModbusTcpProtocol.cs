using System;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Models;
using CommSdk.Protocols.Modbus.Models;
using CommSdk.Protocols.Modbus.Utils;

namespace CommSdk.Protocols.Modbus
{
    public class ModbusTcpProtocol : IProtocol, IProtocolFrameParser, IProtocolResponseValidator
    {
        public string Name { get { return "modbus-tcp"; } }

        public byte[] Encode(IProtocolRequest request)
        {
            var modbusRequest = request as ModbusRequest;
            if (modbusRequest == null) throw new ProtocolException("Invalid request type");

            var pdu = ModbusCodec.BuildRequestPdu(modbusRequest);
            var length = (ushort)(pdu.Length + 1);
            var frame = new byte[7 + pdu.Length];
            frame[0] = (byte)(modbusRequest.TransactionId >> 8);
            frame[1] = (byte)(modbusRequest.TransactionId & 0xFF);
            frame[2] = 0;
            frame[3] = 0;
            frame[4] = (byte)(length >> 8);
            frame[5] = (byte)(length & 0xFF);
            frame[6] = modbusRequest.SlaveId;
            Buffer.BlockCopy(pdu, 0, frame, 7, pdu.Length);
            return frame;
        }

        public IProtocolResponse Decode(byte[] frame)
        {
            if (frame == null || frame.Length < 7) throw new ProtocolException("Invalid TCP frame");
            if (frame[2] != 0 || frame[3] != 0)
                throw new ProtocolException("Unsupported Modbus TCP protocol identifier");

            var transactionId = (ushort)((frame[0] << 8) | frame[1]);
            var length = (ushort)((frame[4] << 8) | frame[5]);
            if (length < 2) throw new ProtocolException("Invalid TCP frame length");

            var expectedLength = 6 + length;
            if (frame.Length != expectedLength)
                throw new ProtocolException("TCP frame length mismatch");

            var unitId = frame[6];
            var pduLength = length - 1;
            var pdu = new byte[pduLength];
            Buffer.BlockCopy(frame, 7, pdu, 0, pdu.Length);
            return ModbusCodec.ParseResponse(unitId, pdu, transactionId);
        }

        public bool TryGetFrameLength(byte[] buffer, int offset, int count, out int frameLength)
        {
            return ModbusFrameParser.TryGetTcpFrameLength(buffer, offset, count, out frameLength);
        }

        public void ValidateResponse(IProtocolRequest request, IProtocolResponse response)
        {
            ModbusResponseValidator.Validate(request as ModbusRequest, response as ModbusResponse, true);
        }
    }
}

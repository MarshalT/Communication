using System;
using CommSdk.Core.Exceptions;
using CommSdk.Protocols.Modbus.Models;

namespace CommSdk.Protocols.Modbus.Utils
{
    internal static class ModbusCodec
    {
        public static byte[] BuildRequestPdu(ModbusRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.FunctionCode == 0) throw new ProtocolException("Function code is required");
            ValidateQuantity(request.FunctionCode, request.Quantity);

            byte[] pdu;
            if (request.Data == null || request.Data.Length == 0)
            {
                if (request.FunctionCode == 0x05 || request.FunctionCode == 0x06 ||
                    request.FunctionCode == 0x0F || request.FunctionCode == 0x10)
                    throw new ProtocolException("Function code requires request data");
                pdu = new byte[5];
                pdu[0] = request.FunctionCode;
                pdu[1] = (byte)(request.Address >> 8);
                pdu[2] = (byte)(request.Address & 0xFF);
                pdu[3] = (byte)(request.Quantity >> 8);
                pdu[4] = (byte)(request.Quantity & 0xFF);
            }
            else
            {
                if (request.Data.Length > 250)
                    throw new ProtocolException("Modbus request data is too large");
                if (request.FunctionCode == 0x10 || request.FunctionCode == 0x0F)
                {
                    var expectedLength = request.FunctionCode == 0x0F
                        ? (request.Quantity + 7) / 8
                        : request.Quantity * 2;
                    if (request.Quantity == 0 || expectedLength != request.Data.Length || request.Data.Length > 255)
                        throw new ProtocolException("Invalid write-multiple data length");
                    pdu = new byte[6 + request.Data.Length];
                    pdu[0] = request.FunctionCode;
                    pdu[1] = (byte)(request.Address >> 8);
                    pdu[2] = (byte)(request.Address & 0xFF);
                    pdu[3] = (byte)(request.Quantity >> 8);
                    pdu[4] = (byte)(request.Quantity & 0xFF);
                    pdu[5] = (byte)request.Data.Length;
                    Buffer.BlockCopy(request.Data, 0, pdu, 6, request.Data.Length);
                }
                else if (request.FunctionCode == 0x05 || request.FunctionCode == 0x06)
                {
                    if (request.Data.Length != 2)
                        throw new ProtocolException("Single-write request data must contain two bytes");

                    pdu = new byte[5];
                    pdu[0] = request.FunctionCode;
                    pdu[1] = (byte)(request.Address >> 8);
                    pdu[2] = (byte)(request.Address & 0xFF);
                    Buffer.BlockCopy(request.Data, 0, pdu, 3, 2);
                }
                else
                {
                    pdu = new byte[1 + 2 + request.Data.Length];
                    pdu[0] = request.FunctionCode;
                    pdu[1] = (byte)(request.Address >> 8);
                    pdu[2] = (byte)(request.Address & 0xFF);
                    Buffer.BlockCopy(request.Data, 0, pdu, 3, request.Data.Length);
                }
            }

            return pdu;
        }

        public static ModbusResponse ParseResponse(byte slaveId, byte[] pdu, ushort transactionId)
        {
            if (pdu == null || pdu.Length < 2) throw new ProtocolException("Invalid PDU");

            var response = new ModbusResponse
            {
                TransactionId = transactionId,
                SlaveId = slaveId,
                FunctionCode = pdu[0]
            };

            if ((pdu[0] & 0x80) != 0)
            {
                if (pdu.Length != 2) throw new ProtocolException("Invalid Modbus exception response");
                response.ExceptionCode = (byte?)pdu[1];
                response.Data = new byte[0];
                return response;
            }

            if (pdu[0] >= 0x01 && pdu[0] <= 0x04)
            {
                if (pdu[1] != pdu.Length - 2)
                    throw new ProtocolException("Modbus response byte count mismatch");
                response.Data = new byte[pdu[1]];
                Buffer.BlockCopy(pdu, 2, response.Data, 0, response.Data.Length);
                return response;
            }
            if ((pdu[0] == 0x05 || pdu[0] == 0x06 || pdu[0] == 0x0F || pdu[0] == 0x10) && pdu.Length != 5)
                throw new ProtocolException("Invalid Modbus write response length");

            if (pdu.Length > 1)
            {
                var data = new byte[pdu.Length - 1];
                Buffer.BlockCopy(pdu, 1, data, 0, data.Length);
                response.Data = data;
            }
            else
            {
                response.Data = new byte[0];
            }

            return response;
        }

        private static void ValidateQuantity(byte functionCode, ushort quantity)
        {
            if (functionCode >= 0x01 && functionCode <= 0x04 && quantity == 0)
                throw new ProtocolException("Quantity must be greater than zero");
            if ((functionCode == 0x01 || functionCode == 0x02) && quantity > 2000)
                throw new ProtocolException("Coil quantity exceeds the Modbus limit");
            if ((functionCode == 0x03 || functionCode == 0x04) && quantity > 125)
                throw new ProtocolException("Register quantity exceeds the Modbus limit");
            if (functionCode == 0x0F && quantity > 1968)
                throw new ProtocolException("Coil write quantity exceeds the Modbus limit");
            if (functionCode == 0x10 && quantity > 123)
                throw new ProtocolException("Register write quantity exceeds the Modbus limit");
        }
    }
}

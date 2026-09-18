using CommSdk.Core.Exceptions;
using CommSdk.Protocols.Modbus.Models;

namespace CommSdk.Protocols.Modbus.Utils
{
    internal static class ModbusResponseValidator
    {
        public static void Validate(ModbusRequest request, ModbusResponse response, bool validateTransactionId)
        {
            if (request == null || response == null)
                throw new ProtocolException("Invalid Modbus request or response");
            if (request.SlaveId != response.SlaveId)
                throw new ProtocolException("Modbus slave identifier mismatch");
            if (validateTransactionId && request.TransactionId != response.TransactionId)
                throw new ProtocolException("Modbus transaction identifier mismatch");

            var exceptionFunction = (byte)(request.FunctionCode | 0x80);
            if (response.IsException)
            {
                if (response.FunctionCode != exceptionFunction)
                    throw new ProtocolException("Modbus exception function mismatch");
                return;
            }

            if (response.FunctionCode != request.FunctionCode)
                throw new ProtocolException("Modbus function code mismatch");

            if (request.FunctionCode >= 0x01 && request.FunctionCode <= 0x04)
            {
                var expectedBytes = request.FunctionCode <= 0x02
                    ? (request.Quantity + 7) / 8
                    : request.Quantity * 2;
                if (response.Data == null || response.Data.Length != expectedBytes)
                    throw new ProtocolException("Modbus response data length mismatch");
            }
        }
    }
}

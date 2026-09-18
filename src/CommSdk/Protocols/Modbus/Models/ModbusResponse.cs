using CommSdk.Core.Models;

namespace CommSdk.Protocols.Modbus.Models
{
    public class ModbusResponse : IProtocolResponse
    {
        public ushort TransactionId { get; set; }
        public byte SlaveId { get; set; }
        public byte FunctionCode { get; set; }
        // For read responses this excludes the Modbus byte-count field.
        public byte[] Data { get; set; }
        public byte? ExceptionCode { get; set; }

        public bool IsException
        {
            get { return ExceptionCode.HasValue; }
        }
    }
}

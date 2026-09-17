using CommSdk.Core.Models;

namespace CommSdk.Protocols.Modbus.Models
{
    public class ModbusRequest : IProtocolRequest
    {
        public ushort TransactionId { get; set; }
        public byte SlaveId { get; set; }
        public byte FunctionCode { get; set; }
        public ushort Address { get; set; }
        public ushort Quantity { get; set; }
        public byte[] Data { get; set; }
    }
}

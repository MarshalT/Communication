using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;

namespace CommSdk.Protocols.Modbus.Factories
{
    public class ModbusRtuProtocolFactory : IProtocolFactory
    {
        public string Type { get { return "modbus-rtu"; } }
        public IProtocol Create(ProtocolConfig config)
        {
            return new ModbusRtuProtocol();
        }
    }
}

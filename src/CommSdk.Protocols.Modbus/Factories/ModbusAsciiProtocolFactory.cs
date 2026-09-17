using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;

namespace CommSdk.Protocols.Modbus.Factories
{
    public class ModbusAsciiProtocolFactory : IProtocolFactory
    {
        public string Type { get { return "modbus-ascii"; } }
        public IProtocol Create(ProtocolConfig config)
        {
            return new ModbusAsciiProtocol();
        }
    }
}

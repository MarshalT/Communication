using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;

namespace CommSdk.Protocols.Modbus.Factories
{
    public class ModbusTcpProtocolFactory : IProtocolFactory
    {
        public string Type { get { return "modbus-tcp"; } }
        public IProtocol Create(ProtocolConfig config)
        {
            return new ModbusTcpProtocol();
        }
    }
}

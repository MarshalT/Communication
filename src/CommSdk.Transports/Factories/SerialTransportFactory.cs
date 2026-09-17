using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;
using CommSdk.Transports.Options;
using System.IO.Ports;

namespace CommSdk.Transports.Factories
{
    public class SerialTransportFactory : ITransportFactory
    {
        public string Type { get { return "serial"; } }

        public ITransport Create(TransportConfig config)
        {
            var parameters = config == null ? null : config.Parameters;
            var options = new SerialTransportOptions
            {
                PortName = TransportConfigParser.GetString(parameters, "port", "COM1"),
                BaudRate = TransportConfigParser.GetInt(parameters, "baud", 9600),
                DataBits = TransportConfigParser.GetInt(parameters, "databits", 8),
                Parity = TransportConfigParser.GetEnum(parameters, "parity", Parity.None),
                StopBits = TransportConfigParser.GetEnum(parameters, "stopbits", StopBits.One),
                ReadTimeoutMs = TransportConfigParser.GetInt(parameters, "readTimeoutMs", 1000),
                WriteTimeoutMs = TransportConfigParser.GetInt(parameters, "writeTimeoutMs", 1000),
                BufferSize = TransportConfigParser.GetInt(parameters, "bufferSize", 4096)
            };

            return new SerialTransport(options);
        }
    }
}

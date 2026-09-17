using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;
using CommSdk.Transports.Options;

namespace CommSdk.Transports.Factories
{
    public class TcpTransportFactory : ITransportFactory
    {
        public string Type { get { return "tcp"; } }

        public ITransport Create(TransportConfig config)
        {
            var parameters = config == null ? null : config.Parameters;
            var options = new TcpTransportOptions
            {
                Host = TransportConfigParser.GetString(parameters, "host", "127.0.0.1"),
                Port = TransportConfigParser.GetInt(parameters, "port", 502),
                ConnectTimeoutMs = TransportConfigParser.GetInt(parameters, "connectTimeoutMs", 3000),
                ReadTimeoutMs = TransportConfigParser.GetInt(parameters, "readTimeoutMs", 1000),
                WriteTimeoutMs = TransportConfigParser.GetInt(parameters, "writeTimeoutMs", 1000),
                BufferSize = TransportConfigParser.GetInt(parameters, "bufferSize", 4096)
            };

            return new TcpTransport(options);
        }
    }
}

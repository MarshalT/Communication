using CommSdk.Core.Abstractions;
using CommSdk.Core.Models;
using CommSdk.Transports.Options;

namespace CommSdk.Transports.Factories
{
    public class UdpTransportFactory : ITransportFactory
    {
        public string Type { get { return "udp"; } }

        public ITransport Create(TransportConfig config)
        {
            var parameters = config == null ? null : config.Parameters;
            var options = new UdpTransportOptions
            {
                Host = TransportConfigParser.GetString(parameters, "host", "127.0.0.1"),
                Port = TransportConfigParser.GetInt(parameters, "port", 502),
                ReceiveTimeoutMs = TransportConfigParser.GetInt(parameters, "receiveTimeoutMs", 1000),
                BufferSize = TransportConfigParser.GetInt(parameters, "bufferSize", 4096)
            };

            return new UdpTransport(options);
        }
    }
}

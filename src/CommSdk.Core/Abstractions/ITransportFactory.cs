using CommSdk.Core.Models;

namespace CommSdk.Core.Abstractions
{
    public interface ITransportFactory
    {
        string Type { get; }
        ITransport Create(TransportConfig config);
    }
}

using CommSdk.Core.Models;

namespace CommSdk.Core.Abstractions
{
    public interface IProtocolFactory
    {
        string Type { get; }
        IProtocol Create(ProtocolConfig config);
    }
}

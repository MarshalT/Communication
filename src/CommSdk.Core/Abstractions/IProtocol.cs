using CommSdk.Core.Models;

namespace CommSdk.Core.Abstractions
{
    public interface IProtocol
    {
        string Name { get; }
        byte[] Encode(IProtocolRequest request);
        IProtocolResponse Decode(byte[] frame);
    }
}

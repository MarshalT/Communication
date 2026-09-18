using CommSdk.Core.Models;

namespace CommSdk.Core.Abstractions
{
    public interface IProtocolResponseValidator
    {
        void ValidateResponse(IProtocolRequest request, IProtocolResponse response);
    }
}

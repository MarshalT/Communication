namespace CommSdk.Core.Abstractions
{
    public interface IProtocolFrameParser
    {
        bool TryGetFrameLength(byte[] buffer, int offset, int count, out int frameLength);
    }
}

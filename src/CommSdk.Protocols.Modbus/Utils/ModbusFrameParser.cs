using CommSdk.Core.Exceptions;

namespace CommSdk.Protocols.Modbus.Utils
{
    internal static class ModbusFrameParser
    {
        public static bool TryGetTcpFrameLength(byte[] buffer, int offset, int count, out int frameLength)
        {
            frameLength = 0;
            if (count < 6) return false;
            if (buffer[offset + 2] != 0 || buffer[offset + 3] != 0)
                throw new ProtocolException("Unsupported Modbus TCP protocol identifier");

            var length = (buffer[offset + 4] << 8) | buffer[offset + 5];
            if (length < 2) throw new ProtocolException("Invalid Modbus TCP length");

            frameLength = 6 + length;
            return count >= frameLength;
        }

        public static bool TryGetRtuFrameLength(byte[] buffer, int offset, int count, out int frameLength)
        {
            frameLength = 0;
            if (count < 2) return false;

            var functionCode = buffer[offset + 1];
            if ((functionCode & 0x80) != 0)
            {
                frameLength = 5;
                return count >= frameLength;
            }

            switch (functionCode)
            {
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                    if (count < 3) return false;
                    frameLength = 5 + buffer[offset + 2];
                    break;
                case 0x05:
                case 0x06:
                case 0x0F:
                case 0x10:
                    frameLength = 8;
                    break;
                default:
                    throw new ProtocolException("Unsupported Modbus RTU function code: " + functionCode);
            }

            return count >= frameLength;
        }

        public static bool TryGetAsciiFrameLength(byte[] buffer, int offset, int count, out int frameLength)
        {
            frameLength = 0;
            if (count < 1) return false;
            if (buffer[offset] != (byte)':')
                throw new ProtocolException("ASCII frame must start with ':'");

            for (var i = offset + 1; i < offset + count - 1; i++)
            {
                if (buffer[i] == (byte)'\r' && buffer[i + 1] == (byte)'\n')
                {
                    frameLength = i - offset + 2;
                    if (frameLength < 9)
                        throw new ProtocolException("ASCII frame is too short");
                    return true;
                }
            }

            return false;
        }
    }
}

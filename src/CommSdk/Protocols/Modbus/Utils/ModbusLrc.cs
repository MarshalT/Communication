namespace CommSdk.Protocols.Modbus.Utils
{
    internal static class ModbusLrc
    {
        public static byte Compute(byte[] data, int offset, int length)
        {
            byte lrc = 0;
            for (int i = offset; i < offset + length; i++)
            {
                lrc += data[i];
            }
            lrc = (byte)((byte)(-lrc) & 0xFF);
            return lrc;
        }
    }
}

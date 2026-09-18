using System;
using System.Text;

namespace CommSdk.Protocols.Modbus.Utils
{
    internal static class HexUtils
    {
        public static string ToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 2);
            for (int i = 0; i < data.Length; i++)
                sb.AppendFormat("{0:X2}", data[i]);
            return sb.ToString();
        }

        public static byte[] FromHex(string hex)
        {
            if (hex == null) throw new ArgumentNullException("hex");
            if (hex.Length % 2 != 0) throw new ArgumentException("Hex length must be even");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }
    }
}

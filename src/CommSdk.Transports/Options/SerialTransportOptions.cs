using System.IO.Ports;

namespace CommSdk.Transports.Options
{
    public class SerialTransportOptions
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public int ReadTimeoutMs { get; set; }
        public int WriteTimeoutMs { get; set; }
        public int BufferSize { get; set; }
    }
}

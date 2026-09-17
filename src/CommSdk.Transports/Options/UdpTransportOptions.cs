namespace CommSdk.Transports.Options
{
    public class UdpTransportOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int ReceiveTimeoutMs { get; set; }
        public int BufferSize { get; set; }
    }
}

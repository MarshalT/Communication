namespace CommSdk.Transports.Options
{
    public class TcpTransportOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int ConnectTimeoutMs { get; set; }
        public int ReadTimeoutMs { get; set; }
        public int WriteTimeoutMs { get; set; }
        public int BufferSize { get; set; }
    }
}

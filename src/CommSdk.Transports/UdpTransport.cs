using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Logging;
using CommSdk.Transports.Options;

namespace CommSdk.Transports
{
    public class UdpTransport : ITransport
    {
        private readonly UdpTransportOptions _options;
        private UdpClient _client;
        private IPEndPoint _remote;
        private readonly object _sync = new object();

        public UdpTransport(UdpTransportOptions options)
        {
            _options = options;
            TimeoutMs = options != null && options.ReceiveTimeoutMs > 0 ? options.ReceiveTimeoutMs : 1000;
            BufferSize = options != null && options.BufferSize > 0 ? options.BufferSize : 4096;
        }

        public string Name { get { return "udp"; } }
        public bool IsOpen { get { return _client != null; } }
        public int TimeoutMs { get; set; }
        public int BufferSize { get; set; }

        public void Open()
        {
            if (_options == null) throw new TransportException("Udp options required");
            if (IsOpen) return;

            try
            {
                LogManager.Current.Info("UdpTransport", "Opening UDP socket to " + _options.Host + ":" + _options.Port);
                var addresses = Dns.GetHostAddresses(_options.Host);
                if (addresses == null || addresses.Length == 0)
                    throw new SocketException((int)SocketError.HostNotFound);

                _remote = new IPEndPoint(addresses[0], _options.Port);
                var client = new UdpClient(addresses[0].AddressFamily);
                client.Client.ReceiveTimeout = EffectiveTimeout(TimeoutMs);
                client.Client.ReceiveBufferSize = Math.Max(1024, BufferSize);
                _client = client;
            }
            catch (Exception ex)
            {
                LogManager.Current.Error("UdpTransport", "Failed to open UDP socket", ex);
                throw new TransportException("Failed to open UDP socket", ex);
            }
        }

        public void Close()
        {
            lock (_sync)
            {
                if (_client == null) return;
                try
                {
                    LogManager.Current.Info("UdpTransport", "Closing UDP socket");
                    _client.Close();
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("UdpTransport", "Failed to close UDP socket", ex);
                    throw new TransportException("Failed to close UDP socket", ex);
                }
                finally
                {
                    _client = null;
                    _remote = null;
                }
            }
        }

        public void Send(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            var client = GetOpenClient();

            try
            {
                client.Send(payload, payload.Length, _remote);
            }
            catch (Exception ex)
            {
                LogManager.Current.Error("UdpTransport", "Failed to send UDP data", ex);
                throw new TransportException("Failed to send UDP data", ex);
            }
        }

        public byte[] Receive()
        {
            var client = GetOpenClient();
            try
            {
                client.Client.ReceiveTimeout = EffectiveTimeout(TimeoutMs);
                var anyAddress = _remote.AddressFamily == AddressFamily.InterNetworkV6
                    ? IPAddress.IPv6Any
                    : IPAddress.Any;
                var ep = new IPEndPoint(anyAddress, 0);
                return client.Receive(ref ep);
            }
            catch (Exception ex)
            {
                LogManager.Current.Error("UdpTransport", "Failed to receive UDP data", ex);
                throw new TransportException("Failed to receive UDP data", ex);
            }
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Run(() => Open(), cancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (payload == null) throw new ArgumentNullException("payload");
            var client = GetOpenClient();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await client.SendAsync(payload, payload.Length, _remote).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) throw;
                LogManager.Current.Error("UdpTransport", "Failed to send UDP data", ex);
                throw new TransportException("Failed to send UDP data", ex);
            }
        }

        public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var client = GetOpenClient();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                client.Client.ReceiveTimeout = EffectiveTimeout(TimeoutMs);
                var receiveTask = client.ReceiveAsync();
                var delayTask = Task.Delay(EffectiveTimeout(TimeoutMs), cancellationToken);
                var completed = await Task.WhenAny(receiveTask, delayTask).ConfigureAwait(false);
                if (completed != receiveTask)
                {
                    AbortPendingReceive(client);
                    try { await receiveTask.ConfigureAwait(false); } catch { }
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("UDP receive timeout");
                }
                return (await receiveTask.ConfigureAwait(false)).Buffer;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) throw;
                LogManager.Current.Error("UdpTransport", "Failed to receive UDP data", ex);
                throw new TransportException("Failed to receive UDP data", ex);
            }
        }

        public void Dispose()
        {
            Close();
        }

        private UdpClient GetOpenClient()
        {
            var client = _client;
            if (client == null) throw new TransportException("UDP not open");
            return client;
        }

        private static int EffectiveTimeout(int timeout)
        {
            return timeout > 0 ? timeout : 1000;
        }

        private void AbortPendingReceive(UdpClient client)
        {
            lock (_sync)
            {
                if (!ReferenceEquals(_client, client)) return;
                try { client.Close(); }
                finally
                {
                    _client = null;
                    _remote = null;
                }
            }
        }
    }
}

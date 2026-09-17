using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Logging;
using CommSdk.Transports.Options;

namespace CommSdk.Transports
{
    public class TcpTransport : ITransport
    {
        private readonly TcpTransportOptions _options;
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly object _sync = new object();

        public TcpTransport(TcpTransportOptions options)
        {
            _options = options;
            TimeoutMs = options != null && options.ReadTimeoutMs > 0 ? options.ReadTimeoutMs : 1000;
            BufferSize = options != null && options.BufferSize > 0 ? options.BufferSize : 4096;
        }

        public string Name { get { return "tcp"; } }
        public bool IsOpen { get { return _client != null && _stream != null && _client.Connected; } }
        public int TimeoutMs { get; set; }
        public int BufferSize { get; set; }

        public void Open()
        {
            if (_options == null) throw new TransportException("Tcp options required");
            if (IsOpen) return;

            var client = new TcpClient();
            try
            {
                LogManager.Current.Info("TcpTransport", "Connecting to " + _options.Host + ":" + _options.Port);
                var result = client.BeginConnect(_options.Host, _options.Port, null, null);
                using (result.AsyncWaitHandle)
                {
                    if (!result.AsyncWaitHandle.WaitOne(EffectiveTimeout(_options.ConnectTimeoutMs)))
                        throw new TimeoutException("TCP connect timeout");
                    client.EndConnect(result);
                }

                var stream = client.GetStream();
                stream.ReadTimeout = EffectiveTimeout(TimeoutMs);
                stream.WriteTimeout = EffectiveTimeout(TimeoutMs);
                _client = client;
                _stream = stream;
            }
            catch (Exception ex)
            {
                client.Close();
                _client = null;
                _stream = null;
                LogManager.Current.Error("TcpTransport", "Failed to open TCP connection", ex);
                throw new TransportException("Failed to open TCP connection", ex);
            }
        }

        public void Close()
        {
            lock (_sync)
            {
                if (_client == null && _stream == null) return;
                try
                {
                    LogManager.Current.Info("TcpTransport", "Closing TCP connection");
                    if (_stream != null) _stream.Close();
                    if (_client != null) _client.Close();
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("TcpTransport", "Failed to close TCP connection", ex);
                    throw new TransportException("Failed to close TCP connection", ex);
                }
                finally
                {
                    _stream = null;
                    _client = null;
                }
            }
        }

        public void Send(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            var stream = GetOpenStream();

            lock (_sync)
            {
                try
                {
                    stream.WriteTimeout = EffectiveTimeout(TimeoutMs);
                    stream.Write(payload, 0, payload.Length);
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("TcpTransport", "Failed to send TCP data", ex);
                    throw new TransportException("Failed to send TCP data", ex);
                }
            }
        }

        public byte[] Receive()
        {
            var stream = GetOpenStream();

            lock (_sync)
            {
                try
                {
                    stream.ReadTimeout = EffectiveTimeout(TimeoutMs);
                    var buffer = new byte[Math.Max(1, BufferSize)];
                    var count = stream.Read(buffer, 0, buffer.Length);
                    if (count <= 0) return new byte[0];

                    var result = new byte[count];
                    Buffer.BlockCopy(buffer, 0, result, 0, count);
                    return result;
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("TcpTransport", "Failed to receive TCP data", ex);
                    throw new TransportException("Failed to receive TCP data", ex);
                }
            }
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_options == null) throw new TransportException("Tcp options required");
            cancellationToken.ThrowIfCancellationRequested();
            if (IsOpen) return;

            var client = new TcpClient();
            try
            {
                var connectTask = client.ConnectAsync(_options.Host, _options.Port);
                var timeoutTask = Task.Delay(EffectiveTimeout(_options.ConnectTimeoutMs), cancellationToken);
                var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completed != connectTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("TCP connect timeout");
                }
                await connectTask.ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var stream = client.GetStream();
                stream.ReadTimeout = EffectiveTimeout(TimeoutMs);
                stream.WriteTimeout = EffectiveTimeout(TimeoutMs);
                lock (_sync)
                {
                    if (IsOpen)
                    {
                        stream.Close();
                        client.Close();
                        return;
                    }
                    _client = client;
                    _stream = stream;
                }
            }
            catch (Exception ex)
            {
                client.Close();
                if (ex is OperationCanceledException) throw;
                LogManager.Current.Error("TcpTransport", "Failed to open TCP connection", ex);
                throw new TransportException("Failed to open TCP connection", ex);
            }
        }

        public async Task SendAsync(byte[] payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (payload == null) throw new ArgumentNullException("payload");
            var stream = GetOpenStream();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                stream.WriteTimeout = EffectiveTimeout(TimeoutMs);
                await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) throw;
                LogManager.Current.Error("TcpTransport", "Failed to send TCP data", ex);
                throw new TransportException("Failed to send TCP data", ex);
            }
        }

        public async Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var stream = GetOpenStream();
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                stream.ReadTimeout = EffectiveTimeout(TimeoutMs);
                var buffer = new byte[Math.Max(1, BufferSize)];
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    var readTask = stream.ReadAsync(buffer, 0, buffer.Length, linked.Token);
                    var timeoutTask = Task.Delay(EffectiveTimeout(TimeoutMs), linked.Token);
                    var completed = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);
                    if (completed != readTask)
                    {
                        linked.Cancel();
                        try { await readTask.ConfigureAwait(false); } catch { }
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new TimeoutException("TCP receive timeout");
                    }
                    linked.Cancel();
                    var count = await readTask.ConfigureAwait(false);
                    if (count <= 0) return new byte[0];
                    var result = new byte[count];
                    Buffer.BlockCopy(buffer, 0, result, 0, count);
                    return result;
                }
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException) throw;
                LogManager.Current.Error("TcpTransport", "Failed to receive TCP data", ex);
                throw new TransportException("Failed to receive TCP data", ex);
            }
        }

        public void Dispose()
        {
            Close();
        }

        private NetworkStream GetOpenStream()
        {
            var stream = _stream;
            if (stream == null || !IsOpen) throw new TransportException("TCP not connected");
            return stream;
        }

        private static int EffectiveTimeout(int timeout)
        {
            return timeout > 0 ? timeout : 1000;
        }
    }
}

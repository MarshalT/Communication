using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Logging;
using CommSdk.Transports.Options;

namespace CommSdk.Transports
{
    public class SerialTransport : ITransport
    {
        private readonly SerialTransportOptions _options;
        private SerialPort _port;
        private readonly object _sync = new object();

        public SerialTransport(SerialTransportOptions options)
        {
            _options = options;
            TimeoutMs = options != null && options.ReadTimeoutMs > 0 ? options.ReadTimeoutMs : 1000;
            BufferSize = options != null && options.BufferSize > 0 ? options.BufferSize : 4096;
        }

        public string Name { get { return "serial"; } }
        public bool IsOpen { get { return _port != null && _port.IsOpen; } }
        public int TimeoutMs { get; set; }
        public int BufferSize { get; set; }

        public void Open()
        {
            if (_options == null) throw new TransportException("Serial options required");
            if (IsOpen) return;

            var port = new SerialPort
            {
                PortName = _options.PortName,
                BaudRate = _options.BaudRate,
                DataBits = _options.DataBits,
                Parity = _options.Parity,
                StopBits = _options.StopBits,
                ReadTimeout = EffectiveTimeout(_options.ReadTimeoutMs),
                WriteTimeout = EffectiveTimeout(_options.WriteTimeoutMs)
            };

            try
            {
                LogManager.Current.Info("SerialTransport", "Opening serial port " + _options.PortName);
                port.Open();
                _port = port;
            }
            catch (Exception ex)
            {
                port.Dispose();
                LogManager.Current.Error("SerialTransport", "Failed to open serial port", ex);
                throw new TransportException("Failed to open serial port", ex);
            }
        }

        public void Close()
        {
            lock (_sync)
            {
                var port = _port;
                if (port == null) return;
                _port = null;
                try
                {
                    LogManager.Current.Info("SerialTransport", "Closing serial port");
                    port.Close();
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("SerialTransport", "Failed to close serial port", ex);
                    throw new TransportException("Failed to close serial port", ex);
                }
                finally
                {
                    port.Dispose();
                }
            }
        }

        public void Send(byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            var port = GetOpenPort();

            lock (_sync)
            {
                try
                {
                    port.Write(payload, 0, payload.Length);
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("SerialTransport", "Failed to send serial data", ex);
                    throw new TransportException("Failed to send serial data", ex);
                }
            }
        }

        public void Send(string payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            Send(Encoding.UTF8.GetBytes(payload));
        }

        public byte[] Receive()
        {
            var port = GetOpenPort();

            lock (_sync)
            {
                try
                {
                    var buffer = new byte[Math.Max(1, BufferSize)];
                    var first = port.ReadByte();
                    if (first < 0) return new byte[0];
                    buffer[0] = (byte)first;
                    var count = 1;

                    while (count < buffer.Length && port.BytesToRead > 0)
                    {
                        var read = port.Read(buffer, count, Math.Min(buffer.Length - count, port.BytesToRead));
                        if (read <= 0) break;
                        count += read;
                    }

                    var result = new byte[count];
                    Buffer.BlockCopy(buffer, 0, result, 0, count);
                    return result;
                }
                catch (TimeoutException ex)
                {
                    LogManager.Current.Warn("SerialTransport", "Serial receive timeout", ex);
                    throw new TransportException("Serial receive timeout", ex);
                }
                catch (Exception ex)
                {
                    LogManager.Current.Error("SerialTransport", "Failed to receive serial data", ex);
                    throw new TransportException("Failed to receive serial data", ex);
                }
            }
        }

        public Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunAsync(Open, cancellationToken);
        }

        public Task SendAsync(byte[] payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunAsync(() => Send(payload), cancellationToken);
        }

        public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return RunAsync(Receive, cancellationToken);
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_port == null) return;
                try
                {
                    if (_port.IsOpen) _port.Close();
                }
                finally
                {
                    _port.Dispose();
                    _port = null;
                }
            }
        }

        private SerialPort GetOpenPort()
        {
            var port = _port;
            if (port == null || !port.IsOpen) throw new TransportException("Serial port not open");
            port.ReadTimeout = EffectiveTimeout(TimeoutMs);
            port.WriteTimeout = EffectiveTimeout(TimeoutMs);
            return port;
        }

        private static int EffectiveTimeout(int timeout)
        {
            return timeout > 0 ? timeout : 1000;
        }

        private static Task RunAsync(Action action, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                action();
            }, cancellationToken);
        }

        private static Task<T> RunAsync<T>(Func<T> action, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return action();
            }, cancellationToken);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;
using CommSdk.Core.Logging;
using CommSdk.Core.Models;

namespace CommSdk.Core.Managers
{
    public class CommClient : IDisposable
    {
        private readonly ITransport _transport;
        private readonly IProtocol _protocol;
        private readonly CommClientOptions _options;
        private readonly SemaphoreSlim _requestGate = new SemaphoreSlim(1, 1);
        private readonly object _receiveSync = new object();
        private readonly List<byte> _receiveBuffer = new List<byte>();
        private int _disposed;

        public CommClient(ITransport transport, IProtocol protocol)
            : this(transport, protocol, null)
        {
        }

        public CommClient(ITransport transport, IProtocol protocol, CommClientOptions options)
        {
            if (transport == null) throw new ArgumentNullException("transport");
            if (protocol == null) throw new ArgumentNullException("protocol");

            _transport = transport;
            _protocol = protocol;
            _options = options ?? new CommClientOptions();
            if (_options.RetryCount < 0) _options.RetryCount = 0;
            if (_options.RetryDelayMs < 0) _options.RetryDelayMs = 0;
            if (_options.TimeoutMs < 0) _options.TimeoutMs = 0;
        }

        public ITransport Transport { get { return _transport; } }
        public IProtocol Protocol { get { return _protocol; } }
        public bool IsOpen { get { return _transport.IsOpen; } }

        public void Open()
        {
            ThrowIfDisposed();
            ApplyTimeout();
            if (!_transport.IsOpen) _transport.Open();
        }

        public async Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            ApplyTimeout();
            if (!_transport.IsOpen)
                await _transport.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Close()
        {
            ThrowIfDisposed();
            lock (_receiveSync) _receiveBuffer.Clear();
            _transport.Close();
        }

        public IProtocolResponse Send(IProtocolRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            ThrowIfDisposed();

            _requestGate.Wait();
            try
            {
                return SendWithRetry(request);
            }
            finally
            {
                _requestGate.Release();
            }
        }

        public async Task<IProtocolResponse> SendAsync(
            IProtocolRequest request,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null) throw new ArgumentNullException("request");
            ThrowIfDisposed();

            await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await SendWithRetryAsync(request, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _requestGate.Release();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            try
            {
                lock (_receiveSync) _receiveBuffer.Clear();
                if (_options.CloseOnDispose) _transport.Dispose();
            }
            finally
            {
                _requestGate.Dispose();
            }
        }

        private IProtocolResponse SendWithRetry(IProtocolRequest request)
        {
            Exception last = null;
            for (var attempt = 0; attempt <= _options.RetryCount; attempt++)
            {
                try
                {
                    EnsureOpen();
                    return SendOnce(request);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException || !IsTransient(ex) || attempt >= _options.RetryCount)
                        throw;

                    last = ex;
                    Reconnect();
                    SleepBeforeRetry(attempt);
                }
            }

            throw last ?? new CommException("Communication failed");
        }

        private async Task<IProtocolResponse> SendWithRetryAsync(
            IProtocolRequest request,
            CancellationToken cancellationToken)
        {
            Exception last = null;
            for (var attempt = 0; attempt <= _options.RetryCount; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await EnsureOpenAsync(cancellationToken).ConfigureAwait(false);
                    return await SendOnceAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException || !IsTransient(ex) || attempt >= _options.RetryCount)
                        throw;

                    last = ex;
                    Reconnect();
                    await DelayBeforeRetryAsync(attempt, cancellationToken).ConfigureAwait(false);
                }
            }

            throw last ?? new CommException("Communication failed");
        }

        private IProtocolResponse SendOnce(IProtocolRequest request)
        {
            var frame = _protocol.Encode(request);
            _transport.Send(frame);
            var response = _protocol.Decode(ReceiveFrame());
            ValidateResponse(request, response);
            return response;
        }

        private async Task<IProtocolResponse> SendOnceAsync(
            IProtocolRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var frame = _protocol.Encode(request);
            await _transport.SendAsync(frame, cancellationToken).ConfigureAwait(false);
            var response = _protocol.Decode(await ReceiveFrameAsync(cancellationToken).ConfigureAwait(false));
            ValidateResponse(request, response);
            return response;
        }

        private void ValidateResponse(IProtocolRequest request, IProtocolResponse response)
        {
            var validator = _protocol as IProtocolResponseValidator;
            if (validator != null) validator.ValidateResponse(request, response);
        }

        private byte[] ReceiveFrame()
        {
            var parser = _protocol as IProtocolFrameParser;
            if (parser == null)
            {
                var chunk = _transport.Receive();
                if (chunk == null || chunk.Length == 0) throw new TransportException("Empty response frame");
                return chunk;
            }

            while (true)
            {
                var frame = TryExtractFrame(parser);
                if (frame != null) return frame;

                var chunk = _transport.Receive();
                if (chunk == null || chunk.Length == 0) throw new TransportException("Empty response frame");
                AppendReceived(chunk);
            }
        }

        private async Task<byte[]> ReceiveFrameAsync(CancellationToken cancellationToken)
        {
            var parser = _protocol as IProtocolFrameParser;
            if (parser == null)
            {
                var chunk = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (chunk == null || chunk.Length == 0) throw new TransportException("Empty response frame");
                return chunk;
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var frame = TryExtractFrame(parser);
                if (frame != null) return frame;

                var chunk = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (chunk == null || chunk.Length == 0) throw new TransportException("Empty response frame");
                AppendReceived(chunk);
            }
        }

        private byte[] TryExtractFrame(IProtocolFrameParser parser)
        {
            lock (_receiveSync)
            {
                if (_receiveBuffer.Count == 0) return null;

                var snapshot = _receiveBuffer.ToArray();
                int frameLength;
                if (!parser.TryGetFrameLength(snapshot, 0, snapshot.Length, out frameLength))
                    return null;
                if (frameLength <= 0 || frameLength > _receiveBuffer.Count)
                    throw new ProtocolException("Protocol returned an invalid frame length");

                var frame = _receiveBuffer.GetRange(0, frameLength).ToArray();
                _receiveBuffer.RemoveRange(0, frameLength);
                return frame;
            }
        }

        private void AppendReceived(byte[] chunk)
        {
            lock (_receiveSync)
            {
                var max = Math.Max(65541, Math.Max(1, _transport.BufferSize) * 16);
                if (_receiveBuffer.Count + chunk.Length > max)
                    throw new ProtocolException("Receive buffer exceeded its maximum size");
                _receiveBuffer.AddRange(chunk);
            }
        }

        private void EnsureOpen()
        {
            if (_options.AutoOpen && !_transport.IsOpen) Open();
        }

        private async Task EnsureOpenAsync(CancellationToken cancellationToken)
        {
            if (_options.AutoOpen && !_transport.IsOpen)
                await OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        private void Reconnect()
        {
            try
            {
                _transport.Close();
            }
            catch (Exception ex)
            {
                LogManager.Current.Warn("CommClient", "Failed to close transport before retry", ex);
            }

            lock (_receiveSync) _receiveBuffer.Clear();
        }

        private void ApplyTimeout()
        {
            if (_options.TimeoutMs > 0) _transport.TimeoutMs = _options.TimeoutMs;
        }

        private void SleepBeforeRetry(int attempt)
        {
            var delay = RetryDelay(attempt);
            if (delay > 0) Thread.Sleep(delay);
        }

        private Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
        {
            var delay = RetryDelay(attempt);
            return delay > 0 ? Task.Delay(delay, cancellationToken) : Task.CompletedTask;
        }

        private int RetryDelay(int attempt)
        {
            var delay = _options.RetryDelayMs;
            if (!_options.ExponentialBackoff) return delay;
            for (var i = 0; i < attempt && delay < 30000; i++)
                delay = (int)Math.Min(30000L, (long)delay * 2L);
            return delay;
        }

        private static bool IsTransient(Exception exception)
        {
            return exception is TransportException || exception is TimeoutException;
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0)
                throw new ObjectDisposedException("CommClient");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommSdk.Core.Abstractions;
using CommSdk.Core.Exceptions;

namespace CommSdk.Tests
{
    public sealed class FakeTransport : ITransport
    {
        private readonly Queue<byte[]> _chunks = new Queue<byte[]>();

        public bool IsOpen { get; private set; }
        public int TimeoutMs { get; set; } = 1000;
        public int BufferSize { get; set; } = 4096;
        public int OpenCount { get; private set; }
        public int CloseCount { get; private set; }
        public int FailSendCount { get; set; }
        public IList<byte[]> Sent { get; } = new List<byte[]>();

        public string Name { get { return "fake"; } }

        public void Enqueue(params byte[][] chunks)
        {
            foreach (var chunk in chunks) _chunks.Enqueue(chunk);
        }

        public void Open()
        {
            IsOpen = true;
            OpenCount++;
        }

        public void Close()
        {
            IsOpen = false;
            CloseCount++;
        }

        public void Send(byte[] payload)
        {
            if (!IsOpen) throw new TransportException("fake transport is closed");
            if (FailSendCount > 0)
            {
                FailSendCount--;
                throw new TransportException("planned send failure");
            }
            var copy = new byte[payload.Length];
            Buffer.BlockCopy(payload, 0, copy, 0, payload.Length);
            Sent.Add(copy);
        }

        public byte[] Receive()
        {
            if (_chunks.Count == 0) throw new TransportException("no fake response available");
            return _chunks.Dequeue();
        }

        public Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Open();
            return Task.CompletedTask;
        }

        public Task SendAsync(byte[] payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Send(payload);
            return Task.CompletedTask;
        }

        public Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Receive());
        }

        public void Dispose()
        {
            Close();
        }
    }
}

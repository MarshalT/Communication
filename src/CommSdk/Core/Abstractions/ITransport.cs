using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommSdk.Core.Abstractions
{
    public interface ITransport : IDisposable
    {
        string Name { get; }
        bool IsOpen { get; }
        int TimeoutMs { get; set; }
        int BufferSize { get; set; }

        void Open();
        void Close();
        void Send(byte[] payload);
        // Text payloads are encoded as UTF-8 before being sent.
        void Send(string payload);
        byte[] Receive();

        Task OpenAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task SendAsync(byte[] payload, CancellationToken cancellationToken = default(CancellationToken));
        Task<byte[]> ReceiveAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}

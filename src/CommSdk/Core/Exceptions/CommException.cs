using System;

namespace CommSdk.Core.Exceptions
{
    public class CommException : Exception
    {
        public CommException(string message, Exception inner = null) : base(message, inner) { }
    }

    public class TransportException : CommException
    {
        public TransportException(string message, Exception inner = null) : base(message, inner) { }
    }

    public class ProtocolException : CommException
    {
        public ProtocolException(string message, Exception inner = null) : base(message, inner) { }
    }

    public class DeviceException : CommException
    {
        public DeviceException(string message, Exception inner = null) : base(message, inner) { }
    }
}

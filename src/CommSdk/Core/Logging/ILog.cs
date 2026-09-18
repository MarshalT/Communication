using System;

namespace CommSdk.Core.Logging
{
    public interface ILog
    {
        void Log(LogLevel level, string source, string message, Exception exception = null);
    }
}

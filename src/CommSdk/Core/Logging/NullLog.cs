using System;

namespace CommSdk.Core.Logging
{
    public sealed class NullLog : ILog
    {
        public static readonly NullLog Instance = new NullLog();
        private NullLog() { }

        public void Log(LogLevel level, string source, string message, Exception exception = null)
        {
        }
    }
}

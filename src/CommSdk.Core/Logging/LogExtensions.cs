using System;

namespace CommSdk.Core.Logging
{
    public static class LogExtensions
    {
        public static void Trace(this ILog log, string source, string message)
        {
            log.Log(LogLevel.Trace, source, message, null);
        }

        public static void Debug(this ILog log, string source, string message)
        {
            log.Log(LogLevel.Debug, source, message, null);
        }

        public static void Info(this ILog log, string source, string message)
        {
            log.Log(LogLevel.Info, source, message, null);
        }

        public static void Warn(this ILog log, string source, string message, Exception ex = null)
        {
            log.Log(LogLevel.Warn, source, message, ex);
        }

        public static void Error(this ILog log, string source, string message, Exception ex = null)
        {
            log.Log(LogLevel.Error, source, message, ex);
        }

        public static void Fatal(this ILog log, string source, string message, Exception ex = null)
        {
            log.Log(LogLevel.Fatal, source, message, ex);
        }
    }
}

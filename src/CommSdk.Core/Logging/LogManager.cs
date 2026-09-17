using System;
using CommSdk.Core.Models;

namespace CommSdk.Core.Logging
{
    public static class LogManager
    {
        private static ILog _current = NullLog.Instance;

        public static ILog Current
        {
            get { return _current; }
            set { _current = value ?? NullLog.Instance; }
        }

        public static void Configure(LoggingConfig config)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.Level)) return;
            LogLevel level;
            if (!Enum.TryParse(config.Level, true, out level)) return;

            var console = Current as ConsoleLog;
            if (console != null) console.MinimumLevel = level;
        }
    }
}

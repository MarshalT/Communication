using System;

namespace CommSdk.Core.Logging
{
    public class ConsoleLog : ILog
    {
        public ConsoleLog()
            : this(LogLevel.Trace)
        {
        }

        public ConsoleLog(LogLevel minimumLevel)
        {
            MinimumLevel = minimumLevel;
        }

        public LogLevel MinimumLevel { get; set; }

        public void Log(LogLevel level, string source, string message, Exception exception = null)
        {
            if (level < MinimumLevel) return;
            var text = string.Format("[{0:yyyy-MM-dd HH:mm:ss.fff}] {1} {2} - {3}",
                DateTime.Now, level.ToString().ToUpperInvariant(), source, message);

            if (exception != null)
                text = text + " | " + exception.GetType().Name + ": " + exception.Message;

            Console.WriteLine(text);
        }
    }
}

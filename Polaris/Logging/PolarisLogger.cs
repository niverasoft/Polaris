using Nivera.Logging;

using System.Collections.Generic;

namespace Polaris.Logging
{
    public class PolarisLogger : ILogger
    {
        private ILogger _internalLogger;

        public LogBuilder Builder => new LogBuilder(this);

        public List<ILogger> Loggers { get; } = new List<ILogger>();

        public PolarisLogger(ILogger internalLogger)
        {
            _internalLogger = internalLogger;
        }

        public void WriteBuilder(LogBuilder logBuilder)
        {
            _internalLogger?.WriteBuilder(logBuilder);

            foreach (var logger in Loggers)
                logger.WriteBuilder(logBuilder);
        }

        public void WriteLine(LogLine logLine)
        {
            _internalLogger?.WriteLine(logLine);

            foreach (var logger in Loggers)
                logger.WriteLine(logLine);
        }
    }
}

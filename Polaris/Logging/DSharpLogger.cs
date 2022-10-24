using System;

using Microsoft.Extensions.Logging;

using NiveraLib.Logging;

using Polaris.Config;

namespace Polaris.Logging
{
    public class DSharpLogger : ILoggerFactory, Microsoft.Extensions.Logging.ILogger
    {
        public void AddProvider(ILoggerProvider provider) { }
        public IDisposable BeginScope<TState>(TState state) { return null; }
        public void Dispose() { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug ? GlobalConfig.Instance.Debug : (logLevel == LogLevel.Trace ? GlobalConfig.Instance.Verbose : true);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string msg = formatter(state, exception);

            // either spammy or useless
            if (logLevel == LogLevel.None || 
                (eventId.Id >= 114 && eventId.Id <= 116) || eventId.Id == 304 ||
                (eventId.Id >= 104 && eventId.Id <=  107) || eventId.Id == 109 ||
                eventId.Id == 117 || eventId.Id == 118 || eventId.Id == 119 ||
                (eventId.Id >= 123 && eventId.Id <= 127) || eventId.Id == 406 ||
                eventId.Id == 407 || eventId.Id == 403)
                return;

            if (logLevel == LogLevel.Critical)
                NiveraLib.Log.SendFatal(msg, new LogId(eventId.Name, eventId.Id));

            if (logLevel == LogLevel.Error)
                NiveraLib.Log.SendError(msg, new LogId(eventId.Name, eventId.Id));

            if (logLevel == LogLevel.Information)
                NiveraLib.Log.SendInfo(msg, new LogId(eventId.Name, eventId.Id));

            if (logLevel == LogLevel.Debug)
                NiveraLib.Log.SendDebug(msg, new LogId(eventId.Name, eventId.Id));

            if (logLevel == LogLevel.Warning)
                NiveraLib.Log.SendWarn(msg, new LogId(eventId.Name, eventId.Id));

            if (logLevel == LogLevel.Trace)
                NiveraLib.Log.SendTrace(msg, new LogId(eventId.Name, eventId.Id));
        }
    }
}
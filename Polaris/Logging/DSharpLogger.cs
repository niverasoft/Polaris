using System;

using Microsoft.Extensions.Logging;

using Polaris.Config;

namespace Polaris.Logging
{
    public class DSharpLogger : ILoggerFactory, ILogger
    {
        public DSharpLogger()
        {
            Nivera.Log.JoinCategory("discord");
        }

        public void AddProvider(ILoggerProvider provider) { }
        public IDisposable BeginScope<TState>(TState state) { return null; }
        public void Dispose() { }
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Debug ? GlobalConfig.Instance.Debug : (logLevel == LogLevel.Trace ? GlobalConfig.Instance.Verbose : true);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string msg = formatter(state, exception);

            if (logLevel == LogLevel.None || logLevel == LogLevel.Trace || 
                ((eventId.Name == "RatelimitDiag" || eventId.Name == "RatelimitPreemptive" || eventId.Name == "VoiceKeepalive") 
                && (eventId.Id == 115 || eventId.Id == 116 || eventId.Id == 304)))
                return;

            if (logLevel == LogLevel.Error)
                Nivera.Log.Error($"[ {eventId.Name} / {eventId.Id} ] {msg}");

            if (logLevel == LogLevel.Information)
                Nivera.Log.Info($"[ {eventId.Name} / {eventId.Id} ] {msg}");

            if (logLevel == LogLevel.Debug)
                Nivera.Log.Debug($"[ {eventId.Name} / {eventId.Id} ] {msg}");

            if (logLevel == LogLevel.Warning)
                Nivera.Log.Warn($"[ {eventId.Name} / {eventId.Id} ] {msg}");

            if (logLevel == LogLevel.Critical)
                Nivera.Log.Error($"[ {eventId.Name} / {eventId.Id} ] {msg}");
        }
    }
}

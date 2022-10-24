using System.Threading.Tasks;

using DSharpPlus.Entities;

using Polaris.Config;

using NiveraLib.Logging;

using System;
using System.Collections.Concurrent;

namespace Polaris.Logging
{
    public class DiscordLogger : ILogger
    {
        private ConcurrentQueue<LogWriter> _queuedLogs = new ConcurrentQueue<LogWriter>();

        public DiscordChannel Channel { get; private set; }

        public LogWriter LogWriter => new LogWriter(this);

        public void SetLogChannel(DiscordChannel channel)
        {
            if (channel == null)
                return;

            if (channel.Id != GlobalConfig.Instance.DiscordLogOutputChannelId)
                return;

            Channel = channel;

            while (_queuedLogs.TryDequeue(out var log))
            {
                Write(log);
            }

            _queuedLogs.Clear();
            _queuedLogs = null;
        }

        public ILogger CopyLogger()
        {
            return new DiscordLogger()
            {
                Channel = Channel,
            };
        }

        public void Destroy()
        {
            Channel = null;
        }

        public LogId GenerateId(string name)
        {
            return LogIdGenerator.GenerateId(name);
        }

        public LogId GenerateId(Type type, string name)
        {
            return LogIdGenerator.GenerateId(type, name);
        }

        public LogId GenerateId<T>(string name)
        {
            return LogIdGenerator.GenerateId<T>(name);
        }

        public bool Write(LogWriter logWriter)
        {
            if (Channel == null)
            {
                _queuedLogs.Enqueue(logWriter);

                return false;
            }

            Task.Run(async () =>
            {
                await Channel.SendMessageAsync(ToString(logWriter));
            });

            return true;
        }

        public bool Write(object text, ConsoleColor color)
        {
            if (Channel == null)
            {
                _queuedLogs.Enqueue(new LogWriter()
                    .WriteLine(text, color));

                return false;
            }

            Task.Run(async () =>
            {
                await Channel.SendMessageAsync(text.ToString());
            });

            return true;
        }

        public bool Write(Tuple<object, ConsoleColor> log)
        {
            if (Channel == null)
            {
                _queuedLogs.Enqueue(new LogWriter()
                    .WriteLine(log.Item1, log.Item2));

                return false;
            }

            Task.Run(async () =>
            {
                await Channel.SendMessageAsync(log.Item1.ToString());
            });

            return true;
        }

        private string ToString(LogWriter logWriter)
        {
            string str = "";

            foreach (var logLine in logWriter.Lines)
            {
                str += $"{logLine.Item1} ";
            }

            return str;
        }
    }
}

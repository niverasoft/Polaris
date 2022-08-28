using System.Collections.Generic;
using System.Threading.Tasks;

using DSharpPlus.Entities;

using Polaris.Config;
using Polaris.Discord;

using Nivera;
using Nivera.Logging;

namespace Polaris.Logging
{
    public class DiscordLogger : ILogger
    {
        private DiscordChannel _logChannel;
        private List<string> _tagMentions = new List<string>();
        private string _roleMention;

        private DiscordColor _warnColor = new DiscordColor("#FFAA00");
        private DiscordColor _errorColor = new DiscordColor("#FF0000");
        private DiscordColor _infoColor = new DiscordColor("#A1F6D3");
        private DiscordColor _debugColor = new DiscordColor("#00FFFB");
        private DiscordColor _verboseColor = new DiscordColor("#00D1FF");

        public LogBuilder Builder => new LogBuilder(this);

        public bool ChannelFound => _logChannel != null;

        public DiscordLogger()
        {
            Log.JoinCategory("logging/discord");
        }

        public async Task FindChannel(ulong channelId)
        {
            if (channelId == 0 || !GlobalConfig.Instance.AllowDiscordLogOutput)
            {
                Log.Info("Discord log output is disabled.");

                return;
            }

            _logChannel = await DiscordNetworkHandlers.GlobalClient.GetChannelAsync(channelId);

            if (_logChannel == null)
                Log.Error("Failed to find the Discord log channel.");
            else
                Log.Info($"Found the Discord log channel: {_logChannel.Name} in {_logChannel.Guild.Name}");

            if (GlobalConfig.Instance.DiscordPingIds.Count > 0)
            {
                foreach (var id in GlobalConfig.Instance.DiscordPingIds)
                {
                    _roleMention += $" <@&{id}>";
                }

                Log.Info($"Role mention set!\n{_roleMention}");
            }
        }

        public void WriteBuilder(LogBuilder logBuilder)
        {
            WriteLine(logBuilder.GetLine());
        }

        public void WriteLine(LogLine logLine)
        {
            if (_logChannel == null)
                return;

            string msg = GetMessageString(logLine);
            string tag = GetDiscordTag(msg);
            DiscordColor color = GetColor(msg);

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            builder.WithAuthor("Polaris Console Output");
            builder.WithTitle(tag);
            builder.WithColor(color);
            builder.WithDescription($"```yaml\n{msg}\n```");

            if (_tagMentions.Contains(tag) && !string.IsNullOrEmpty(_roleMention))
                _logChannel.SendMessageAsync(_roleMention);

            _logChannel.SendMessageAsync(builder);
        }

        private string GetMessageString(LogLine logLine)
        {
            string str = "";

            foreach (var tuple in logLine.ReadAllLines())
            {
                str += $"{tuple.Item1} ";
            }

            return str;
        }

        private DiscordColor GetColor(string msg)
        {
            msg = msg.ToLower();

            if (msg.Contains("error"))
                return _errorColor;

            if (msg.Contains("warn"))
                return _warnColor;

            if (msg.Contains("info"))
                return _infoColor;

            if (msg.Contains("debug"))
                return _debugColor;

            if (msg.Contains("verbose"))
                return _verboseColor;

            return _infoColor;
        }

        private string GetDiscordTag(string msg)
        {
            msg = msg.ToLower();

            if (msg.Contains("error"))
                return "Error";

            if (msg.Contains("warn"))
                return "Warning";

            if (msg.Contains("info"))
                return "Information";

            if (msg.Contains("debug"))
                return "Debug";

            if (msg.Contains("verbose"))
                return "Verbose";

            return "Unknown Tag";
        }
    }
}

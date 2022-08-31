using System.Collections.Generic;

using Polaris.Entities;

namespace Polaris.Config
{
    public class GlobalConfig
    {
        public static GlobalConfig Instance { get; set; }

        public string BotStatus { get; set; } = "default";

        public string LogTimestampFormat { get; set; } = "t";

        public char DefaultPrefix { get; set; } = '!';

        public ulong BotOwnerId { get; set; }
        public string BotOwnerNickname { get; set; }

        public List<ulong> DiscordPingIds { get; set; } = new List<ulong>();

        public bool Debug { get; set; }
        public bool Verbose { get; set; }
        public bool AllowLavalink { get; set; } = false;
        public bool IncludeSystemDebug { get; set; } = false;
        public bool AllowDiscordLogOutput { get; set; } = false;
        public bool LavalinkAtStart { get; set; } = false;
        public bool CheckPackages { get; set; }
        public bool AllowCustomCommands { get; set; }

        public ulong DiscordLogOutputChannelId { get; set; } = 0;

        public byte[] Token { get; set; }

        public byte[] LavalinkAddress { get; set; }
        public byte[] LavalinkPassword { get; set; }

        public bool NativeExit { get; set; }

        public List<UserPlaylist> UserPlaylists { get; set; } = new List<UserPlaylist>();
    }
}

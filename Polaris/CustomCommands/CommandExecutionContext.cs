using DSharpPlus;
using DSharpPlus.Entities;

namespace Polaris.CustomCommands
{
    public class CommandExecutionContext
    {
        public DiscordMember Author { get; set; }
        public DiscordMessage Message { get; set; }
        public DiscordGuild Guild { get; set; }
        public DiscordClient Client { get; set; }

        public CustomCommand Command { get; set; }

        public string RawArgumentsString { get; set; }
        public string[] RawArguments { get; set; }
    }
}
using System.Collections.Generic;
using System.Linq;

using Polaris.Config;

using DSharpPlus.CommandsNext;

namespace Polaris.Core
{
    public class CoreCollection
    {
        public static List<CoreCollection> ActiveCores = new List<CoreCollection>();

        public CoreCollection(ServerCore serverCore, ServerConfig serverConfig, ulong serverID)
        {
            ServerId = serverID;
            ServerConfig = serverConfig;
            ServerCore = serverCore;

            ServerAdminCore = new ServerAdminCore();
            ServerDailyCore = new ServerDailyCore();
            ServerDatabaseCore = new ServerDatabaseCore();
            ServerFunCore = new ServerFunCore();
            ServerLevelsCore = new ServerLevelsCore();
            ServerLogCore = new ServerLogCore();
            ServerMusicCore = new ServerMusicCore();
            ServerPermsCore = new ServerPermsCore();
            ServerRadioCore = new ServerRadioCore();
            ServerReactionRolesCore = new ServerReactionRolesCore();
            ServerTextToSpeechCore = new ServerTextToSpeechCore();
            ServerUtilityCore = new ServerUtilityCore();
            ServerLavalinkCore = new ServerLavalinkCore();

            ActiveCores.Add(this);
        }

        public ulong ServerId { get; set; }

        public ServerConfig ServerConfig { get; set; }
        public ServerCore ServerCore { get; set; }
        public ServerAdminCore ServerAdminCore { get; set; }
        public ServerDailyCore ServerDailyCore { get; set; }
        public ServerDatabaseCore ServerDatabaseCore { get; set; }
        public ServerFunCore ServerFunCore { get; set; }
        public ServerLevelsCore ServerLevelsCore { get; set; }
        public ServerLogCore ServerLogCore { get; set; }
        public ServerMusicCore ServerMusicCore { get; set; }
        public ServerPermsCore ServerPermsCore { get; set; }
        public ServerRadioCore ServerRadioCore { get; set; }
        public ServerReactionRolesCore ServerReactionRolesCore { get; set; }
        public ServerTextToSpeechCore ServerTextToSpeechCore { get; set; }
        public ServerUtilityCore ServerUtilityCore { get; set; }
        public ServerLavalinkCore ServerLavalinkCore { get; set; }

        public static CoreCollection Get(ulong server)
        {
            var core = ActiveCores.FirstOrDefault(x => x.ServerId == server);

            if (core == null)
                throw new KeyNotFoundException("server");

            return core;
        }

        public static CoreCollection Get(CommandContext ctx)
            => Get(ctx.Guild.Id);
    }
}

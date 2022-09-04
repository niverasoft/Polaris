using System.Collections.Generic;
using System.Linq;

using Polaris.Config;

using DSharpPlus.CommandsNext;

namespace Polaris.Core
{
    public class CoreCollection
    {
        public static List<CoreCollection> ActiveCores = new List<CoreCollection>();

        public CoreCollection(ServerCore serverCore, ServerConfig serverConfig, ServerCache serverCache, ulong serverID)
        {
            ServerId = serverID;
            ServerConfig = serverConfig;
            ServerCore = serverCore;
            ServerCache = serverCache;

            ServerAdminCore = new ServerAdminCore();
            ServerDailyCore = new ServerDailyCore();
            ServerDatabaseCore = new ServerDatabaseCore();
            ServerFunCore = new ServerFunCore();
            ServerLevelsCore = new ServerLevelsCore();
            ServerLogCore = new ServerLogCore();
            ServerPermsCore = new ServerPermsCore();
            ServerRadioCore = new ServerRadioCore(serverID);
            ServerReactionRolesCore = new ServerReactionRolesCore();
            ServerTextToSpeechCore = new ServerTextToSpeechCore();
            ServerUtilityCore = new ServerUtilityCore();
            ServerLavalinkCore = new ServerLavalinkCore(serverID);

            ActiveCores.Add(this);
        }

        public ulong ServerId { get; set; }

        public ServerConfig ServerConfig { get; set; }
        public ServerCache ServerCache { get; set; }
        public ServerCore ServerCore { get; set; }
        public ServerAdminCore ServerAdminCore { get; set; }
        public ServerDailyCore ServerDailyCore { get; set; }
        public ServerDatabaseCore ServerDatabaseCore { get; set; }
        public ServerFunCore ServerFunCore { get; set; }
        public ServerLevelsCore ServerLevelsCore { get; set; }
        public ServerLogCore ServerLogCore { get; set; }
        public ServerPermsCore ServerPermsCore { get; set; }
        public ServerRadioCore ServerRadioCore { get; set; }
        public ServerReactionRolesCore ServerReactionRolesCore { get; set; }
        public ServerTextToSpeechCore ServerTextToSpeechCore { get; set; }
        public ServerUtilityCore ServerUtilityCore { get; set; }
        public ServerLavalinkCore ServerLavalinkCore { get; set; }

        public static CoreCollection Get(ulong server)
        {
            return ActiveCores.FirstOrDefault(x => x.ServerId == server);
        }

        public static CoreCollection Get(CommandContext ctx)
            => Get(ctx.Guild.Id);
    }
}
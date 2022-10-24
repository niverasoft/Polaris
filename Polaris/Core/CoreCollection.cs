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
            ServerPermsCore = new ServerPermsCore();
            ServerMusicCore = new ServerMusicCore(ServerId);

            ActiveCores.Add(this);
        }

        public ulong ServerId { get; set; }

        public ServerConfig ServerConfig { get; set; }
        public ServerCache ServerCache { get; set; }
        public ServerCore ServerCore { get; set; }
        public ServerAdminCore ServerAdminCore { get; set; }
        public ServerPermsCore ServerPermsCore { get; set; }
        public ServerMusicCore ServerMusicCore { get; set; }

        public static CoreCollection Get(ulong server)
        {
            return ActiveCores.FirstOrDefault(x => x.ServerId == server);
        }

        public static CoreCollection Get(CommandContext ctx)
            => Get(ctx.Guild.Id);
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

using Polaris.Config;
using Polaris.Boot;
using Polaris.Discord;

using Nivera;
using Polaris.Entities;

namespace Polaris.Core
{
    public class ServerCore
    {
        public int CoreId { get; set; }

        public DiscordClient Client;
        public DiscordGuild Guild;

        public CoreCollection CoreCollection;

        public bool LastAnnouncementSent;

        public async Task LoadAsync(ServerConfig serverConfig, ServerCache serverCache, DiscordClient client, DiscordGuild guild)
        {
            try
            {
                CoreCollection = new CoreCollection(this, serverConfig, serverCache, guild.Id);
                Client = client;
                Guild = guild;
                CoreId = GetNextServerCoreId();

                Log.JoinCategory($"cores/server");

                ConfigManager.ConfigList[Guild.Id] = CoreCollection.ServerConfig;
                ConfigManager.CacheList[Guild.Id] = CoreCollection.ServerCache;
                ConfigManager.Save();

                InstallEventHandlers();

                if (!Program.DiscordLogger.ChannelFound && GlobalConfig.Instance.DiscordLogOutputChannelId != 0 && GlobalConfig.Instance.AllowDiscordLogOutput)
                    await Program.DiscordLogger.FindChannel(GlobalConfig.Instance.DiscordLogOutputChannelId);

                Log.Info($"Caching Discord members for {guild.Name}");

                CachedDiscordMember.Cache(Guild.CurrentMember);

                foreach (var member in guild.Members.Values)
                {
                    CachedDiscordMember.Cache(member);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public async Task LoadAsync(DiscordClient client, DiscordGuild guild)
        {
            try
            {
                CoreCollection = new CoreCollection(this, new ServerConfig(), new ServerCache(), guild.Id);
                CoreCollection.ServerConfig.BotOwner = guild.Owner != null ? guild.Owner.Id : guild.Members.Where(x => x.Value.Roles.Any(z => z.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed)).First().Key;
                Client = client;
                Guild = guild;
                CoreId = GetNextServerCoreId();

                Log.JoinCategory($"cores/server");

                ConfigManager.ConfigList[Guild.Id] = CoreCollection.ServerConfig;
                ConfigManager.CacheList[Guild.Id] = CoreCollection.ServerCache;
                ConfigManager.Save();

                InstallEventHandlers();

                if (!Program.DiscordLogger.ChannelFound && GlobalConfig.Instance.DiscordLogOutputChannelId != 0 && GlobalConfig.Instance.AllowDiscordLogOutput)
                    await Program.DiscordLogger.FindChannel(GlobalConfig.Instance.DiscordLogOutputChannelId);

                Log.Info($"Caching Discord members for {guild.Name}");

                CachedDiscordMember.Cache(Guild.CurrentMember);

                foreach (var member in guild.Members.Values)
                {
                    CachedDiscordMember.Cache(member);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void InstallEventHandlers()
        {
            Client.MessageCreated += (x, e) =>
            {
                try
                {
                    if (e.Guild.Id != Guild.Id)
                        return Task.CompletedTask;

                    var cnext = DiscordNetworkHandlers.GlobalClient.GetCommandsNext();
                    var msg = e.Message;

                    var cmdStart = msg.GetStringPrefixLength(CoreCollection.ServerConfig.Prefix.ToString());

                    if (cmdStart == -1)
                        return Task.CompletedTask;

                    var prefix = msg.Content.Substring(0, cmdStart);
                    var cmdString = msg.Content.Substring(cmdStart);

                    var command = cnext.FindCommand(cmdString, out var args);

                    if (command == null)
                        return Task.CompletedTask;

                    var ctx = cnext.CreateContext(msg, prefix, command, args);

                    Task.Run(async () => await cnext.ExecuteCommandAsync(ctx));
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                return Task.CompletedTask;
            };
        }

        public static int GetNextServerCoreId()
            => DiscordNetworkHandlers.ServerCores.Count + 1;
    }
}
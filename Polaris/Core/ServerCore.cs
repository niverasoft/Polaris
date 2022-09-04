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
using Polaris.CustomCommands;
using Microsoft.VisualBasic;

namespace Polaris.Core
{
    public class ServerCore
    {
        public int CoreId { get; set; }

        public DiscordClient Client { get; set; }
        public DiscordGuild Guild { get; set; }

        public CoreCollection CoreCollection { get; set; }

        public bool LastAnnouncementSent { get; set; }

        public void Load(ServerConfig serverConfig, ServerCache serverCache, DiscordClient client, DiscordGuild guild)
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
                    Task.Run(async () => await Program.DiscordLogger.FindChannel(GlobalConfig.Instance.DiscordLogOutputChannelId));

                Log.Info($"Caching Discord members for {guild.Name}");

                foreach (var member in guild.Members.Values)
                {
                    Task.Run(() => CachedDiscordMember.Cache(member));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Load(DiscordClient client, DiscordGuild guild)
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
                    Task.Run(async () => await Program.DiscordLogger.FindChannel(GlobalConfig.Instance.DiscordLogOutputChannelId));

                Log.Info($"Caching Discord members for {guild.Name}");

                foreach (var member in guild.Members.Values)
                {
                    Task.Run(() => CachedDiscordMember.Cache(member));
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
                Task.Run(async () =>
                {
                    try
                    {
                        if (e.Guild.Id != Guild.Id)
                            return;

                        var cmdIndex = e.Message.GetStringPrefixLength(CoreCollection.ServerConfig.Prefix);

                        if (cmdIndex == -1)
                            cmdIndex = e.Message.GetMentionPrefixLength(e.Guild.CurrentMember);

                        if (cmdIndex == -1)
                            return;

                        var prefix = e.Message.Content.Substring(0, cmdIndex);
                        var cmd = e.Message.Content.Substring(cmdIndex);
                        var cmdObj = DiscordNetworkHandlers.CommandsNextExtension.FindCommand(cmd, out var args);

                        if (cmdObj == null)
                        {
                            if (CustomCommandManager.IsEnabled && CustomCommandManager.TryGetCommand(cmd, out var customCommand))
                            {
                                await CustomCommandManager.TryInvokeCommand(args ?? "", e.Guild.Members[e.Author.Id], e.Message, customCommand);

                                return;
                            }

                            return;
                        }

                        await DiscordNetworkHandlers.CommandsNextExtension.ExecuteCommandAsync(
                            DiscordNetworkHandlers.CommandsNextExtension.CreateContext(
                                e.Message,
                                prefix,
                                cmdObj,
                                args));
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                });

                return Task.CompletedTask;
            };
        }

        public static int GetNextServerCoreId()
            => DiscordNetworkHandlers.ServerCores.Count + 1;
    }
}
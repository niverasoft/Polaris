using System;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

using Polaris.Config;
using Polaris.Boot;
using Polaris.Discord;
using Polaris.Entities;

using NiveraLib;
using NiveraLib.Logging;

namespace Polaris.Core
{
    public class ServerCore
    {
        private LogId logId;

        public int CoreId { get; set; }

        public DiscordClient Client { get; set; }
        public DiscordGuild Guild { get; set; }

        public CoreCollection CoreCollection { get; set; }

        public bool LastAnnouncementSent { get; set; }

        public void Load(ServerConfig serverConfig, ServerCache serverCache, DiscordClient client, DiscordGuild guild)
        {
            try
            {
                logId = new LogId($"cores / main / {guild.Id}");
                CoreCollection = new CoreCollection(this, serverConfig, serverCache, guild.Id);
                Client = client;
                Guild = guild;
                CoreId = GetNextServerCoreId();

                ConfigManager.ConfigList[Guild.Id] = CoreCollection.ServerConfig;
                ConfigManager.CacheList[Guild.Id] = CoreCollection.ServerCache;
                ConfigManager.Save();

                InstallEventHandlers();
              
                if (Program.DiscordLogger.Channel == null)
                {
                    if (GlobalConfig.Instance.DiscordLogOutputChannelId != 0)
                    {
                        if (GlobalConfig.Instance.AllowDiscordLogOutput)
                        {
                            if (guild.Channels.Any(x => x.Key == GlobalConfig.Instance.DiscordLogOutputChannelId))
                            {
                                Program.DiscordLogger.SetLogChannel(guild.Channels[GlobalConfig.Instance.DiscordLogOutputChannelId]);
                            }
                        }
                    }
                }

                //foreach (var member in guild.Members.Values)
                //    Task.Run(() => CachedDiscordMember.Cache(member));
            }
            catch (Exception ex)
            {
                Log.SendError(ex);
            }
        }

        public void Load(DiscordClient client, DiscordGuild guild)
        {
            try
            {
                logId = new LogId($"cores / {CoreId} / {guild.Id}");
                CoreCollection = new CoreCollection(this, new ServerConfig(), new ServerCache(), guild.Id);
                Client = client;
                Guild = guild;
                CoreId = GetNextServerCoreId();

                ConfigManager.ConfigList[Guild.Id] = CoreCollection.ServerConfig;
                ConfigManager.CacheList[Guild.Id] = CoreCollection.ServerCache;
                ConfigManager.Save();

                InstallEventHandlers();

                if (Program.DiscordLogger.Channel == null)
                {
                    if (GlobalConfig.Instance.DiscordLogOutputChannelId != 0)
                    {
                        if (GlobalConfig.Instance.AllowDiscordLogOutput)
                        {
                            if (guild.Channels.Any(x => x.Key == GlobalConfig.Instance.DiscordLogOutputChannelId))
                            {
                                Program.DiscordLogger.SetLogChannel(guild.Channels[GlobalConfig.Instance.DiscordLogOutputChannelId]);
                            }
                        }
                    }
                }

                //foreach (var member in guild.Members.Values)
                //    Task.Run(() => CachedDiscordMember.Cache(member));
            }
            catch (Exception ex)
            {
                Log.SendError(ex);
            }
        }

        public void Destroy()
        {

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
                            return;

                        await DiscordNetworkHandlers.CommandsNextExtension.ExecuteCommandAsync(
                            DiscordNetworkHandlers.CommandsNextExtension.CreateContext(
                                e.Message,
                                prefix,
                                cmdObj,
                                args));
                    }
                    catch (Exception ex)
                    {
                        Log.SendError(ex, logId);
                    }
                });

                return Task.CompletedTask;
            };
        }

        public static int GetNextServerCoreId()
            => DiscordNetworkHandlers.ServerCores.Count + 1;
    }
}
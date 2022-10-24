using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Executors;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.Net.Models;
using DSharpPlus.Net.Serialization;
using DSharpPlus.Net.Udp;
using DSharpPlus.Net.WebSocket;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using DSharpPlus.VoiceNext.EventArgs;

using Polaris.Config;
using Polaris.Boot;
using Polaris.Core;
using Polaris.Discord;
using Polaris.Entities;
using Polaris.Enums;
using Polaris.Helpers;
using Polaris.Properties;
using Polaris.Exceptions;

using NiveraLib;
using NiveraLib.Logging;

namespace Polaris.Discord
{
    public static class DiscordNetworkHandlers
    {
        private static LogId logId = new LogId("handlers / discord", 111);
        private static bool HasLoaded;

        public static DiscordClient GlobalClient;
        public static CommandsNextExtension CommandsNextExtension;
        public static InteractivityExtension InteractivityExtension;
        public static LavalinkExtension LavalinkExtension;
        public static VoiceNextExtension VoiceNextExtension;

        public static List<ServerCore> ServerCores = new List<ServerCore>();

        public static async Task SendAnnouncementAsync()
        {
            if (ConfigManager.HasAnnouncement)
            {
                while (!ServerCores.All(x => x.LastAnnouncementSent))
                {
                    await Task.Delay(100);

                    foreach (var core in ServerCores)
                    {
                        if (core.LastAnnouncementSent)
                            continue;

                        await core.Guild.SystemChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                            .WithColor(ColorPicker.InfoColor)
                            .WithAuthor("Update Log")
                            .AddEmoteAuthor(EmotePicker.LoudspeakerEmote)
                            .WithDescription(ConfigManager.Announcement));

                        core.LastAnnouncementSent = true;
                    }
                }

                ConfigManager.Announcement = null;
                ConfigManager.HasAnnouncement = false;

                if (File.Exists($"{Directory.GetCurrentDirectory()}/announcement.txt"))
                    File.Delete($"{Directory.GetCurrentDirectory()}/announcement.txt");
            }
        }

        public static async Task InstallAsync()
        {
            try
            {
                if (GlobalConfig.Instance.Token == null || GlobalConfig.Instance.Token.Length <= 0)
                {
                    Log.SendError($"You need to set the bot token with the -token launch argument! Exiting in 10 seconds ..", logId);

                    await Task.Delay(10000);

                    Program.Kill("Missing bot token.");
                }

                if (ConfigManager.HasAnnouncement)
                {
                    Log.SendInfo("Pending announcement loaded.");
                }

                Log.SendInfo($"Please wait, trying to connect to Discord ..", logId);

                GlobalClient = new DiscordClient(new DiscordConfiguration
                {
                    AlwaysCacheMembers = true,
                    Intents = DiscordIntents.All,
                    LogTimestampFormat = GlobalConfig.Instance.LogTimestampFormat,
                    MessageCacheSize = 8096,
                    MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace,
                    Token = Encoding.UTF32.GetString(GlobalConfig.Instance.Token),
                    TokenType = TokenType.Bot,
                    LoggerFactory = new Logging.DSharpLogger(),
                    GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                });

                GlobalClient.GuildAvailable += (x, e) =>
                {
                    try
                    {
                        if (ConfigManager.ConfigList.TryGetValue(e.Guild.Id, out var config))
                        {
                            Log.SendInfo("Creating a Server Core for a known server.", logId);

                            var score = new ServerCore();

                            score.Load(config, ConfigManager.CacheList.TryGetValue(e.Guild.Id, out var cache) ? cache : new ServerCache(), x, e.Guild);

                            ServerCores.Add(score);

                            if (!HasLoaded)
                                OnReady();
                        }
                        else
                        {
                            Log.SendInfo("Creating a Server Core for a new server.", logId);

                            var core = new ServerCore();

                            core.Load(x, e.Guild);

                            ServerCores.Add(core);

                            if (!HasLoaded)
                                OnReady();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SendError(ex);
                    }

                    return Task.CompletedTask;
                };

                GlobalClient.Resumed += (e, x) =>
                {
                    Log.SendWarn("Session was resumed!", logId);

                    Task.Run(() =>
                    {
                        ServerCores.ForEach(x => x.Destroy());
                        ServerCores.Clear();

                        OnReady();
                    });

                    return Task.CompletedTask;
                };

                CommandsNextExtension = GlobalClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    UseDefaultCommandHandler = false
                });

                InteractivityExtension = GlobalClient.UseInteractivity(new InteractivityConfiguration
                {

                });

                VoiceNextExtension = GlobalClient.UseVoiceNext(new VoiceNextConfiguration
                {
                    AudioFormat = new AudioFormat(48000, 2, VoiceApplication.LowLatency),
                    EnableIncoming = true,
                    PacketQueueSize = 1024
                });

                LavalinkExtension = GlobalClient.UseLavalink();

                CommandsNextExtension.RegisterCommands<GlobalCommandCore>();

                CommandsNextExtension.CommandErrored += async (x, e) =>
                {
                    Log.SendError($"Command errored! {e.Exception} ({e.Command.Name} - {e.Context.RawArgumentString ?? "No arguments"})", logId);

                    if (e.Exception != null)
                    {
                        if (e.Exception is MissingArgumentsException)
                        {
                            await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder()
                                .WithAuthor("Command Failed")
                                .WithTitle("It seems like you forgot some arguments.")
                                .AddField("Command Usage", $"", false)
                                .AddField("Arguments", $"", false)
                                .MakeError());
                        }
                        else
                        {
                            await e.Context.Channel.SendMessageAsync(new DiscordEmbedBuilder()
                                .WithAuthor("Command SendError")
                                .WithTitle("Unknown SendError")
                                .WithDescription(e.Exception.Message)
                                .MakeError());
                        }
                    }
                };

                await GlobalClient.ConnectAsync();
                await GlobalClient.InitializeAsync();
            }
            catch (Exception ex)
            {
                Log.SendError(ex);
            }
        }

        public static void OnReady()
        {
            Task.Run(async () =>
            {
                try
                {
                    if (GlobalConfig.Instance.BotStatus == "default" || string.IsNullOrEmpty(GlobalConfig.Instance.BotStatus))
                        await GlobalClient.UpdateStatusAsync(new DiscordActivity
                        {
                            ActivityType = ActivityType.Playing,
                            Name = $"Use {GlobalConfig.Instance.DefaultPrefix}help",
                        }, UserStatus.Idle);
                    else
                        await GlobalClient.UpdateStatusAsync(new DiscordActivity
                        {
                            ActivityType = ActivityType.Playing,
                            Name = GlobalConfig.Instance.BotStatus
                        }, UserStatus.Idle);

                    HasLoaded = true;

                    Log.SendInfo("The bot is ready to operate.", logId);
                }
                catch (Exception ex)
                {
                    Log.SendError(ex);
                }
            });
        }

        public static void Kill()
        {
            GlobalClient.DisconnectAsync();
            GlobalClient.Dispose();
            GlobalClient = null;
        }
    }
}
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

using Nivera;

namespace Polaris.Discord
{
    public static class DiscordNetworkHandlers
    {
        static DiscordNetworkHandlers()
        {
            Log.JoinCategory("discordnet");
        }

        private static bool HasLoaded;

        public static DiscordClient GlobalClient;
        public static CommandsNextExtension CommandsNextExtension;
        public static InteractivityExtension InteractivityExtension;
        public static LavalinkExtension LavalinkExtension;

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

                Log.Verbose("Last announcement deleted.");
            }
        }

        public static async Task InstallAsync()
        {
            if (GlobalConfig.Instance.Token == null || GlobalConfig.Instance.Token.Length <= 0)
            {
                Log.Error($"You need to set the bot token with the -token launch argument! Exiting in 10 seconds ..");
                await Task.Delay(10000);
                Program.Kill("Missing bot token.");
            }

            if (ConfigManager.HasAnnouncement)
            {
                Log.Info("Pending announcement loaded.");
            }

            Log.Info("Installing Discord Network Handlers ..");

            GlobalClient = new DiscordClient(new DiscordConfiguration
            {
                AlwaysCacheMembers = true,
                Intents = DiscordIntents.All,
                LogTimestampFormat = GlobalConfig.Instance.LogTimestampFormat,
                MessageCacheSize = 8096,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Error,
                Token = Encoding.UTF32.GetString(GlobalConfig.Instance.Token),
                TokenType = TokenType.Bot
            });

            GlobalClient.GuildAvailable += async (x, e) =>
            {
                if (ConfigManager.ConfigList.TryGetValue(e.Guild.Id, out var config))
                {
                    Log.Info($"Discovered a known server. {e.Guild.Id} >> {e.Guild.Name}");

                    var score = new ServerCore();

                    await score.LoadAsync(config, x, e.Guild);

                    ServerCores.Add(score);

                    if (!HasLoaded)
                        await OnReady();
                }
                else
                {
                    Log.Info($"A new was server discovered! {e.Guild.Id} >> {e.Guild.Name}");

                    var core = new ServerCore();

                    await core.LoadAsync(x, e.Guild);

                    ServerCores.Add(core);

                    if (!HasLoaded)
                        await OnReady();
                }
            };

            CommandsNextExtension = GlobalClient.UseCommandsNext(new CommandsNextConfiguration
            {
                UseDefaultCommandHandler = false
            });

            InteractivityExtension = GlobalClient.UseInteractivity(new InteractivityConfiguration
            {

            });

            LavalinkExtension = GlobalClient.UseLavalink();

            CommandsNextExtension.RegisterCommands<GlobalCommandCore>();

            CommandsNextExtension.CommandExecuted += (x, e) =>
            {
                Log.Verbose($"Command executed! {e.Command.Name} - {e.Context.RawArgumentString}");

                return Task.CompletedTask;
            };

            CommandsNextExtension.CommandErrored += async (x, e) =>
            {
                Log.Error($"Command errored! {e.Exception} ({e.Command.Name} - {e.Context.RawArgumentString})");

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
                            .WithAuthor("Command Error")
                            .WithTitle("Unknown Error")
                            .WithDescription(e.Exception.Message)
                            .MakeError());
                    }
                }
            };

            await GlobalClient.ConnectAsync();
            await GlobalClient.InitializeAsync();
        }

        public static async Task OnReady()
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

            Log.Info("Ready!");
        }

        public static void Kill()
        {
            GlobalClient.DisconnectAsync();
            GlobalClient.Dispose();
        }
    }
}
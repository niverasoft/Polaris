using System;
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

using Nivera;
using Nivera.Utils;
using Polaris.Pagination;

namespace Polaris.Core
{
    public static class CommandExtensions
    {
        static CommandExtensions()
        {
            Log.JoinCategory("commands");
        }

        public static bool CheckPerms(this CommandContext ctx, string perms, out CoreCollection coreCollection)
        {
            coreCollection = CoreCollection.Get(ctx);

            if (coreCollection == null)
            {
                ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Fatal error - failed to find this server's core collection! Issue reported.")
                    .MakeError());

                Log.Warn($"Failed to find the core collection of {ctx.Guild.Name} - {ctx.Guild.Id}");

                return false;
            }

            bool valid = coreCollection.ServerPermsCore.CheckForPerms(ctx.Member, perms, coreCollection);

            if (!valid)
            {
                if (perms == "owner")
                    ctx.RespondAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Missing permissions!")
                        .WithTitle("This command is reserved for the bot's owner!")
                        .MakeError());
                else
                    ctx.RespondAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Missing permissions!")
                        .AddField("Required Permission", perms)
                        .MakeError());
            }

            return valid;
        }
    }

    public class GlobalCommandCore : BaseCommandModule
    {
        public static string[] FalseValues = new string[] { "n", "no", "nah", "f", "false", "0" };
        public static string[] TrueValues = new string[] { "y", "yes", "ye", "yeah", "true", "t", "1" };

        [Command("prefix")]
        [RequireGuild]
        public async Task SetPrefix(CommandContext ctx, string prefix)
        {
            if (!ctx.CheckPerms("mgmt.prefix", out var coreCollection))
                return;

            coreCollection.ServerConfig.Prefix = prefix;

            ConfigManager.Save();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor($"Done! The prefix for this server is now `{prefix}`")
                .MakeSuccess());
        }

        [Command("announce")]
        public async Task Announce(CommandContext ctx)
        {
            if (!ctx.CheckPerms("owner", out _))
                return;

            if (!ConfigManager.HasAnnouncement)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There are no annoucements pending.")
                    .MakeInfo());

                return;
            }

            await DiscordNetworkHandlers.SendAnnouncementAsync();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor("Announcement sent!")
                .MakeSuccess());
        }

        [RequireGuild]
        [Command("info")]
        [Aliases("botinfo")]
        public async Task BotInfo(CommandContext ctx)
        {
            var cores = CoreCollection.Get(ctx);

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor("Polaris", null, ctx.Guild.CurrentMember.GetAvatarUrl(ImageFormat.Png, 1024))
                .WithColor(DiscordColor.Blue)
                .WithTitle("About me!")
                .WithUrl("https://github.com/niverasoft/Polaris")
                .AddField("Version", $"Polaris ({BuildInfo.Version} / {BuildInfo.Branch})\nGateway (v{ctx.Client.GatewayVersion})", true)
                .AddField("Library", $"DSharpPlus ({ctx.Client.VersionString})\nNiveraLib ({LibProperties.LibraryVersion})", true)
                .AddField("Ping", $"{ctx.Client.Ping} ms", true)
                .AddField("Voice Gateway Ping", $"{CoreCollection.ActiveCores.Select(x => x.ServerRadioCore).FirstOrDefault(x => x.IsConnected)?.WebPing} ms", true)
                .AddField("Core ID", $"P-{cores.ServerCore.CoreId}", true)
                .AddField("Owner", $"<@!{GlobalConfig.Instance.BotOwnerId}>", true)
                .WithTimestamp(DateTimeOffset.Now.ToLocalTime())
                .WithFooter("© Nivera, 2022"));

            if (GlobalConfig.Instance.IncludeSystemDebug)
            {
                await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("System Information Debug")
                    .AddField("Operating System", SystemHelper.Name, true)
                    .AddField("Operating System Version", Environment.OSVersion.Version.ToString(), true)
                    .AddField("Operating System Architecture", SystemHelper.Architecture, true)
                    .AddField("Machine Uptime", new TimeSpan(SystemHelper.Is32Bit ? Environment.TickCount : Environment.TickCount64).ToString(), true)
                    .AddField("App Runtime", SystemHelper.Runtime, true)
                    .AddField("Processor Name", SystemHelper.CpuName, true)
                    .AddField("Processor Cores", $"L{SystemHelper.CpuLogicalCores}, P{SystemHelper.CpuPhysicalCores}", true)
                    .AddField("Processor Frequency", SystemHelper.CpuFrequencyString, true)
                    .AddField("System Memory", $"{SystemHelper.RamFreeString.Replace("MB", "GB")} / {SystemHelper.RamTotalString.Replace("MB", "GB")}", true)
                    .WithFooter("You are seeing this message because you enabled system information debug in the global config.")
                    .MakeInfo());
            }
        }

        [Command("radionowplaying")]
        [Aliases("rnp", "radionp")]
        [RequireGuild]
        public async Task RadioNowPlaying(CommandContext ctx)
        {
            await CoreCollection.Get(ctx)?.ServerRadioCore?.NowPlayingAsync();
        }

        [Command("addradio")]
        [Aliases("ar")]
        public async Task AddRadio(CommandContext ctx, string streamUrl, string dataUrl, [RemainingText] string radioName)
        {
            if (!ctx.CheckPerms("mgmt.radio", out var cores))
                return;

            GlobalCache.Instance.Stations.Add(new RadioStation
            {
                Name = radioName,
                StreamUrl = streamUrl,
                DataUrl = dataUrl
            });

            ConfigManager.Save();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor("Succesfully added a new radio station")
                .AddField("Station Name", radioName, true)
                .AddField("Station URL", streamUrl, true)
                .AddField("Station Data URL", dataUrl, true)
                .MakeSuccess());
        }

        [Command("removeradio")]
        [Aliases("rr")]
        public async Task RemoveRadio(CommandContext ctx, [RemainingText] string radioName)
        {
            if (!ctx.CheckPerms("mgmt.radio", out var cores))
                return;

            var station = GlobalCache.Instance.Stations.FirstOrDefault(s => s.Name.ToLower().Contains(radioName.ToLower()));

            if (station == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find that radio station.")
                    .MakeError());

                return;
            }

            int index = GlobalCache.Instance.Stations.IndexOf(station);

            GlobalCache.Instance.Stations.RemoveAt(index);

            ConfigManager.Save();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor("Succesfully removed radio station.")
                .AddField("Station Name", station.Name, true)
                .AddField("Station URL", station.StreamUrl, true)
                .MakeSuccess());
        }

        [Command("listradio")]
        [Aliases("lr")]
        public async Task ListRadio(CommandContext ctx)
        {
            List<Page> pages = PageParser.SplitToPages(GlobalCache.Instance.Stations, new DiscordEmbedBuilder()
                .WithAuthor($"List of added radio stations ({GlobalCache.Instance.Stations.Count}):")
                .MakeInfo());

            await ctx.Channel.SendPaginatedMessageAsync(ctx.User, pages, PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Ignore);
        }

        [Command("radioplay")]
        [Aliases("rp")]
        [RequireGuild]
        public async Task RadioPlay(CommandContext ctx, [RemainingText]string radioName)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            RadioStation radioStation = GlobalCache.Instance.Stations.FirstOrDefault(x => x.Name.ToLower().Contains(radioName.ToLower()));

            if (radioStation == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find specified radio station.")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerRadioCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A radio station is currently playing.")
                    .MakeError());

                return;
            }

            await coreCollection.ServerRadioCore.JoinAsync(ctx.Member.VoiceState.Channel, ctx.Channel);
            await coreCollection.ServerRadioCore.PlayAsync(radioStation);
        }


        [Command("radiojoin")]
        [Aliases("rj")]
        [RequireGuild]
        public async Task RadioJoin(CommandContext ctx, [RemainingText] string radioName)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerRadioCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A radio station is currently playing.")
                    .MakeError());

                return;
            }

            await coreCollection.ServerRadioCore.JoinAsync(ctx.Member.VoiceState.Channel, ctx.Channel);
        }


        [Command("radioleave")]
        [Aliases("rl")]
        [RequireGuild]
        public async Task RadioLeave(CommandContext ctx, [RemainingText] string radioName)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerRadioCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A radio station is currently playing.")
                    .MakeError());

                return;
            }

            await coreCollection.ServerRadioCore.DisconnectAsync();
        }

        [Command("radiopause")]
        [Aliases("rpa")]
        [RequireGuild]
        public async Task RadioPause(CommandContext ctx, [RemainingText] string radioName)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerRadioCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A radio station is currently playing.")
                    .MakeError());

                return;
            }

            await coreCollection.ServerRadioCore.PauseAsync();
        }

        [Command("radioresume")]
        [Aliases("rres")]
        [RequireGuild]
        public async Task RadioResume(CommandContext ctx, [RemainingText] string radioName)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerRadioCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A radio station is currently playing.")
                    .MakeError());

                return;
            }

            await coreCollection.ServerRadioCore.ResumeAsync();
        }

        [Command("radiostop")]
        [Aliases("rs")]
        [RequireGuild]
        public async Task RadioStop(CommandContext ctx, [RemainingText] string radioName)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerRadioCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A radio station is currently playing.")
                    .MakeError());

                return;
            }

            await coreCollection.ServerRadioCore.StopAsync();
        }

        [Command("join")]
        [RequireGuild]
        public async Task Join(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A song is currently playing.")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.JoinAsync(ctx.Member.VoiceState.Channel, ctx.Channel);
        }

        [Command("leave")]
        [Aliases("dis", "disconnect")]
        [RequireGuild]
        public async Task Leave(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("I am not in a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice == null || coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.LeaveAsync();
        }

        [Command("play")]
        [Aliases("pl")]
        [RequireGuild]
        public async Task Play(CommandContext ctx, [RemainingText] string url)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A song is currently playing.")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.PlayAsync(url, ctx.Member.VoiceState.Channel, ctx.Channel, ctx.User);
        }

        [Command("pause")]
        [Aliases("ps")]
        [RequireGuild]
        public async Task Pause(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.PauseAsync();
        }

        [Command("resume")]
        [Aliases("res")]
        [RequireGuild]
        public async Task Resume(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.ResumeAsync();
        }

        [Command("nowplaying")]
        [Aliases("np")]
        [RequireGuild]
        public async Task NowPlaying(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.NowPlaying();
        }

        [Command("seek")]
        [RequireGuild]
        public async Task Seek(CommandContext ctx, TimeSpan time)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.SeekAsync(time);
        }

        [Command("playpartial")]
        [Aliases("ppl")]
        [RequireGuild]
        public async Task PlayPartial(CommandContext ctx, TimeSpan from, TimeSpan to)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.PlayPartial(from, to);
        }

        [Command("queue")]
        [Aliases("q")]
        [RequireGuild]
        public async Task Queue(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.ViewQueueAsync(ctx.User);
        }

        [Command("clearqueue")]
        [Aliases("cq")]
        [RequireGuild]
        public async Task ClearQueue(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.ClearQueueAsync();
        }

        [Command("loop")]
        [RequireGuild]
        public async Task Loop(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            if (coreCollection.ServerLavalinkCore.IsLooping)
            {
                coreCollection.ServerLavalinkCore.IsLooping = false;

                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Looping disabled.")
                    .WithColor(ColorPicker.SuccessColor)
                    .AddEmoteAuthor(EmotePicker.RepeatEmote));
            }
            else
            {
                coreCollection.ServerLavalinkCore.IsLooping = true;

                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Looping enabled.")
                    .WithColor(ColorPicker.SuccessColor)
                    .AddEmoteAuthor(EmotePicker.RepeatEmote));
            }
        }

        [Command("stop")]
        [RequireGuild]
        public async Task Stop(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.StopAsync();
        }

        [Command("volume")]
        [Aliases("vol")]
        [RequireGuild]
        public async Task Volume(CommandContext ctx, int volume)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A song is currently playing.")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.SetVolumeAsync(volume);
        }

        [Command("skip")]
        [Aliases("s")]
        [RequireGuild]
        public async Task Skip(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Lavalink is disabled in the config!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerLavalinkCore.IsServerConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("The Lavalink Server is not connected!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A song is currently playing.")
                    .MakeError());

                return;
            }

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerLavalinkCore.Voice != null && (coreCollection.ServerLavalinkCore.Voice.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerLavalinkCore.SetTextChannel(ctx.Channel);

            await coreCollection.ServerLavalinkCore.SkipAsync();
        }

        [Command("setstatus")]
        [Aliases("status", "sts")]
        [RequireGuild]
        public async Task SetStatus(CommandContext ctx, [RemainingText] string status)
        {
            if (!ctx.CheckPerms("owner", out _))
                return;

            GlobalConfig.Instance.BotStatus = status;
            ConfigManager.Save();

            if (GlobalConfig.Instance.BotStatus == "default" || string.IsNullOrEmpty(GlobalConfig.Instance.BotStatus))
                await DiscordNetworkHandlers.GlobalClient.UpdateStatusAsync(new DiscordActivity
                {
                    ActivityType = ActivityType.Playing,
                    Name = $"Use {GlobalConfig.Instance.DefaultPrefix}help",
                }, UserStatus.Idle);
            else
                await DiscordNetworkHandlers.GlobalClient.UpdateStatusAsync(new DiscordActivity
                {
                    ActivityType = ActivityType.Playing,
                    Name = GlobalConfig.Instance.BotStatus
                }, UserStatus.Idle);

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor("Polaris is now even better.")
                .MakeSuccess());
        }

        [Command("perms")]
        [RequireGuild]
        public async Task Perms(CommandContext ctx, [RemainingText] string arg)
        {
            try
            {
                var core = CoreCollection.Get(ctx);

                if (arg == null)
                {
                    await ctx.RespondAsync(new DiscordEmbedBuilder()
                        .WithAuthor($"Missing action. (setup/view)")
                        .MakeError());

                    return;
                }

                string[] args = arg.Split(' ');

                switch (args[0].ToLower())
                {
                    case "setup":
                        {
                            if (!ctx.CheckPerms("mgmt.perms", out var coreCollection))
                                return;

                            DiscordMessage discordMessage = await ctx.RespondAsync(new DiscordEmbedBuilder()
                                .WithAuthor("Alright, let's setup your permissions.\nMention the roles or users that you want to manage.")
                                .MakeInfo());

                            var result = await ctx.Channel.GetNextMessageAsync(ctx.Member);

                            if (result.TimedOut)
                            {
                                await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Timed out!")
                                    .MakeError());

                                return;
                            }

                            discordMessage = result.Result;

                            if (discordMessage == null)
                            {
                                await ctx.Channel.SendMessageAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Fatal error - the Interaction object is null.")
                                    .MakeError());

                                return;
                            }

                            List<DiscordRole> roles = StringHelpers.FindRoles(discordMessage.Content, ctx) ?? new List<DiscordRole>();
                            List<DiscordMember> users = StringHelpers.FindUsers(discordMessage.Content, ctx) ?? new List<DiscordMember>();

                            if (roles.Count < 1 && users.Count < 1)
                            {
                                await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Cancelled - failed to find any roles or users.")
                                    .MakeError());

                                return;
                            }

                            discordMessage = await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                .WithDescription($"Mentioned Roles: {(roles.Count > 0 ? $"{string.Join(", ", roles.Select(x => x.Mention))}" : "None")}" +
                                $"\nMentioned Users: {(users.Count > 0 ? $"{string.Join(", ", users.Select(x => x.Mention))}" : "None")}" +
                                $"\nNow, list the permissions you want these role(s)/user(s) to have or use the bot's built-in permission levels.")
                                .WithAuthor("Permissions Setup")
                                .MakeInfo());

                            result = await ctx.Channel.GetNextMessageAsync(ctx.Member);

                            if (result.TimedOut)
                            {
                                await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Timed out!")
                                    .MakeError());

                                return;
                            }

                            discordMessage = result.Result;

                            List<string> perms = PermsHelper.ProcessPermissions(discordMessage.Content);

                            if (perms.Count < 1)
                            {
                                discordMessage = await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("You have not mentioned any valid permission nodes. Would you like to view a list? (yes/no)")
                                    .MakeWarn());

                                result = await ctx.Channel.GetNextMessageAsync(ctx.Member);

                                if (result.TimedOut)
                                {
                                    await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                        .WithAuthor("Timed out!")
                                        .MakeError());

                                    return;
                                }
                                else
                                {
                                    discordMessage = result.Result;

                                    if (TrueValues.Contains(discordMessage.Content))
                                    {
                                        StringBuilder stringBuilder = new StringBuilder();

                                        foreach (var pair in PermsHelper.Permissions)
                                        {
                                            stringBuilder.AppendLine($"**>> {pair.Key}**");
                                            stringBuilder.AppendJoin(", ", pair.Value);
                                            stringBuilder.AppendLine(Environment.NewLine);
                                        }

                                        await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                            .WithAuthor("Permissions Setup")
                                            .WithDescription(stringBuilder.ToString())
                                            .MakeInfo());

                                        discordMessage = await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                                                    .WithAuthor("Permissions Setup")
                                                                    .WithDescription($"Mentioned Roles: {(roles.Count > 0 ? $"{string.Join(", ", roles.Select(x => x.Mention))}" : "None")}" +
                                                                                     $"\nMentioned Users: {(users.Count > 0 ? $"{string.Join(", ", users.Select(x => x.Mention))}" : "None")}" +
                                                                                     $"\nNow, list the permissions you want these role(s)/user(s) to have or use the bot's built-in permission levels.")
                                                                    .MakeInfo());

                                        result = await ctx.Channel.GetNextMessageAsync(ctx.Member);

                                        if (result.TimedOut)
                                        {
                                            await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                                .WithAuthor("Timed out!")
                                                .MakeError());

                                            return;
                                        }

                                        discordMessage = result.Result;

                                        perms = PermsHelper.ProcessPermissions(discordMessage.Content);

                                        if (perms.Count < 1)
                                        {
                                            await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                                .WithAuthor("You have failed to specify any valid permission nodes.")
                                                .MakeError());

                                            return;
                                        }
                                    }
                                    else
                                    {
                                        await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                                .WithAuthor("You have failed to specify any valid permission nodes.")
                                                .MakeError());

                                        return;
                                    }
                                }
                            }

                            discordMessage = await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                .WithAuthor($"All of the roles/users mentioned above will receive these permissions ({perms.Count}). Proceed? (yes/no)")
                                .MakeWarn());

                            result = await ctx.Channel.GetNextMessageAsync(ctx.Member);

                            if (result.TimedOut)
                            {
                                await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Timed out!")
                                    .MakeError());

                                return;
                            }

                            discordMessage = result.Result;

                            if (FalseValues.Contains(discordMessage.Content.ToLower()))
                            {
                                await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Cancelled.")
                                    .MakeInfo());

                                return;
                            }
                            else
                            {
                                if (TrueValues.Contains(discordMessage.Content.ToLower()))
                                {
                                    foreach (var role in roles)
                                        if (role != null)
                                            coreCollection.ServerConfig.Perms[role.Id] = perms;
                                    foreach (var user in users)
                                        if (user != null)
                                            coreCollection.ServerConfig.Perms[user.Id] = perms;
                                    ConfigManager.Save();

                                    await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                        .WithAuthor("Success - new permissions saved.")
                                        .MakeSuccess());

                                    return;
                                }
                                else
                                {
                                    await discordMessage.RespondAsync(new DiscordEmbedBuilder()
                                        .WithAuthor("Cancelled - unknown value.")
                                        .MakeError());

                                    return;
                                }
                            }
                        }

                    case "view":
                        {
                            if (!ctx.CheckPerms("mgmt.perms", out var cores))
                                return;

                            List<DiscordRole> roles = cores.ServerConfig.Perms.Keys.Where(x => ctx.Guild.Roles.Select(z => z.Key).Contains(x)).Select(x => ctx.Guild.GetRole(x)).ToList();
                            List<DiscordMember> users = cores.ServerConfig.Perms.Keys.Where(x => ctx.Guild.Members.Select(z => z.Key).Contains(x)).Select(x => ctx.Guild.Members[x]).ToList();

                            StringBuilder stringBuilder = new StringBuilder();
                            stringBuilder.AppendLine($"**Allow Admin Override: {cores.ServerConfig.AllowAdminOverride}**");
                            stringBuilder.AppendLine($"**Bot Owner: {cores.ServerConfig.BotOwner}**");
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine("**>> Roles**");
                            foreach (var role in roles)
                                stringBuilder.AppendLine($"{role.Mention}: {string.Join(", ", cores.ServerConfig.Perms[role.Id])}");
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine("**>> Members**");
                            foreach (var member in users)
                                stringBuilder.AppendLine($"{member.Mention}: {string.Join(", ", cores.ServerConfig.Perms[member.Id])}");

                            await ctx.RespondAsync(new DiscordEmbedBuilder()
                                .WithAuthor($"Permissions for {ctx.Guild.Name}")
                                .WithDescription(stringBuilder.ToString())
                                .MakeInfo());

                            break;
                        }

                    case "remove":
                        {
                            if (!ctx.CheckPerms("mgmt.perms", out var cores))
                                return;

                            if (args.Length < 2)
                            {
                                await ctx.RespondAsync(new DiscordEmbedBuilder()
                                    .WithAuthor("Missing arguments!")
                                    .AddField("Usage", $"{cores.ServerConfig.Prefix}perms remove <list>")
                                    .AddField("Arguments", "<list> = a list of role/user mentions/IDs to remove, separated by a comma or a space (654554445654,3543546314,3578436344 for example)")
                                    .MakeError());

                                return;
                            }

                            List<DiscordMember> members = StringHelpers.FindUsers(args[1], ctx);
                            List<DiscordRole> roles = StringHelpers.FindRoles(args[1], ctx);

                            foreach (DiscordMember member in members)
                                cores.ServerConfig.Perms.Remove(member.Id);
                            foreach (DiscordRole role in roles)
                                cores.ServerConfig.Perms.Remove(role.Id);

                            ConfigManager.Save();

                            await ctx.RespondAsync(new DiscordEmbedBuilder()
                                .WithAuthor("Succesfully removed!")
                                .MakeSuccess());

                            break;
                        }

                    case "reset":
                        {
                            if (!ctx.CheckPerms("mgmt.perms", out var cores))
                                return;

                            cores.ServerConfig.Perms.Clear();

                            ConfigManager.Save();

                            await ctx.RespondAsync(new DiscordEmbedBuilder()
                                .WithAuthor("All permissions were cleared.")
                                .MakeSuccess());

                            break;
                        }

                    case "list":
                        {
                            StringBuilder stringBuilder = new StringBuilder();

                            foreach (var pair in PermsHelper.Permissions)
                            {
                                stringBuilder.AppendLine($"**>> {pair.Key}**");
                                stringBuilder.AppendJoin(", ", pair.Value);
                                stringBuilder.AppendLine(Environment.NewLine);
                            }

                            await ctx.RespondAsync(new DiscordEmbedBuilder()
                                .WithAuthor("Permissions Setup")
                                .WithDescription(stringBuilder.ToString())
                                .MakeInfo());

                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Nivera.Log.Error(ex);
            }
        }

    }
}
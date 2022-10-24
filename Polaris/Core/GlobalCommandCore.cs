using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

using Polaris.Config;
using Polaris.Boot;
using Polaris.Discord;
using Polaris.Entities;
using Polaris.Helpers;
using Polaris.Pagination;
using Polaris.Properties;

using NiveraLib;
using NiveraLib.Logging;

using Polaris.Helpers.Music;
using System.Collections.Concurrent;

namespace Polaris.Core
{
    public static class CommandExtensions
    {
        public static LogId logId = new LogId("core / commands / extensions", 121);

        public static bool CheckPerms(this CommandContext ctx, string perms, out CoreCollection coreCollection)
        {
            coreCollection = CoreCollection.Get(ctx);

            if (coreCollection == null)
            {
                ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Fatal error - failed to find this server's core collection! Issue reported.")
                    .MakeError());

                Log.SendWarn($"Failed to find the core collection of {ctx.Guild.Name} - {ctx.Guild.Id}");

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

        public static ConcurrentQueue<DiscordMessage> DeleteMusic = new ConcurrentQueue<DiscordMessage>();
    }

    public class GlobalCommandCore : BaseCommandModule
    {
        public static string[] FalseValues = new string[] { "n", "no", "nah", "f", "false", "0" };
        public static string[] TrueValues = new string[] { "y", "yes", "ye", "yeah", "true", "t", "1" };

        [Command("prefix")]
        [Aliases("pref")]
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

        [Command("createplaylist")]
        [Aliases("cp")]
        public async Task CreatePlaylist(CommandContext ctx, string name, string description, string id, params string[] songs)
        {
            List<MusicTrack> tracks = new List<MusicTrack>();

            var message = await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription($"{EmotePicker.LoadingGif} **Loading track(s) ..**")
                .WithColor(ColorPicker.SuccessColor));

            foreach (var song in songs)
            {
                var video = await MusicSearch.Search(song);

                try
                {
                    if (video != null)
                    {
                        if (!(video is TrackSearchResult track))
                            continue;

                        tracks.Add(new MusicTrack
                        {
                            Author = track.SelectedTrack.Author,
                            AuthorUrl = track.SelectedTrack.AuthorUrl,
                            ThumbnailUrl = track.SelectedTrack.Thumbnail,
                            Duration = track.SelectedTrack.Duration,
                            Title = track.SelectedTrack.Title,
                            Url = track.SelectedTrack.Url
                        });
                    }
                }
                catch { }
            }

            UserPlaylist userPlaylist = UserPlaylistHelper.CreatePlaylist(
                name,
                id,
                description ?? "No description.",
                ctx.User.Id,
                ctx.Message.Content.Contains("--global"),
                tracks);

            await message.ModifyAsync(x => x.Embed = new DiscordEmbedBuilder()
                .WithAuthor("Playlist created!")
                .AddField("Playlist Name", userPlaylist.Name, true)
                .AddField("Playlist ID", userPlaylist.ID, true)
                .AddField("Description", userPlaylist.Description, true)
                .AddField("Author", $"<@{userPlaylist.PlaylistOwner}>", true)
                .AddField("Is Private", userPlaylist.IsPrivate.ToString().ToLower(), true)
                .AddField("Songs", userPlaylist.Tracks.Count.ToString(), true)
                .MakeSuccess());
        }

        [Command("viewplaylist")]
        [Aliases("vp")]
        public async Task ViewPlaylist(CommandContext ctx, [RemainingText] string playlist)
        {
            var list = UserPlaylistHelper.RetrievePlaylist(playlist);

            if (list == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find your playlist.")
                    .MakeError());

                return;
            }

            if (list.IsPrivate && ctx.User.Id != list.PlaylistOwner)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("This playlist is set as private by it's owner.")
                    .MakeError());

                return;
            }

            await ctx.Channel.SendPaginatedMessageAsync(
                ctx.User,
                PageParser.SplitToPages(
                    list.Tracks,
                    new DiscordEmbedBuilder()
                    .WithAuthor($"{GlobalCache.GetCachedDiscordMember(list.PlaylistOwner)?.Name ?? "Unknown"}'s playlist - {list.Name}")
                    .WithFooter(list.Description)
                    .MakeInfo()),
                PaginationBehaviour.WrapAround,
                ButtonPaginationBehavior.DeleteButtons);
        }

        [Command("deleteplaylist")]
        [Aliases("dp")]
        public async Task DeletePlaylist(CommandContext ctx, [RemainingText] string playlist)
        {
            var list = UserPlaylistHelper.RetrievePlaylist(playlist);

            if (list == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find your playlist.")
                    .MakeError());

                return;
            }

            if (ctx.User.Id != list.PlaylistOwner)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not the owner of this playlist.")
                    .MakeError());

                return;
            }

            UserPlaylistHelper.DeletePlaylist(list);

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor("Playlist deleted!")
                .MakeSuccess());
        }

        [Command("playplaylist")]
        [Aliases("pp")]
        [RequireGuild]
        public async Task PlayPlaylist(CommandContext ctx, string playlist)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (coreCollection.ServerMusicCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
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

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            var list = UserPlaylistHelper.RetrievePlaylist(playlist);

            if (list == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find your playlist.")
                    .MakeError());

                return;
            }

            if (list.IsPrivate && ctx.User.Id != list.PlaylistOwner)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("This playlist is set as private by it's owner.")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Play(list);
        }

        [Command("addtoplaylist")]
        [Aliases("atp")]
        public async Task AddToPlaylist(CommandContext ctx, string playlist, params string[] songs)
        {
            var list = UserPlaylistHelper.RetrievePlaylist(playlist);

            if (list == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find your playlist.")
                    .MakeError());

                return;
            }

            if (ctx.User.Id != list.PlaylistOwner)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You do not own this playlist.")
                    .MakeError());

                return;
            }

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithDescription($"{EmotePicker.LoadingGif} **Loading track(s) ..**")
                .MakeInfo());

            int curState = list.Tracks.Count;

            foreach (var song in songs)
            {
                var video = await MusicSearch.Search(song);

                try
                {
                    if (video != null)
                    {
                        if (!(video is TrackSearchResult track))
                            continue;

                        list.Tracks.Add(new MusicTrack
                        {
                            Author = track.SelectedTrack.Author,
                            AuthorUrl = track.SelectedTrack.AuthorUrl,
                            ThumbnailUrl = track.SelectedTrack.Thumbnail,
                            Duration = track.SelectedTrack.Duration,
                            Title = track.SelectedTrack.Title,
                            Url = track.SelectedTrack.Url
                        });
                    }
                }
                catch { }
            }

            ConfigManager.Save();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor($"Added {list.Tracks.Count - curState} song(s) to {list.ID}!")
                .MakeSuccess());
        }

        [Command("removeplaylist")]
        [Aliases("rfp")]
        public async Task RemovePlaylist(CommandContext ctx, string playlist, params string[] songs)
        {
            var list = UserPlaylistHelper.RetrievePlaylist(playlist);

            if (list == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Failed to find your playlist.")
                    .MakeError());

                return;
            }

            if (ctx.User.Id != list.PlaylistOwner)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You do not own this playlist.")
                    .MakeError());

                return;
            }

            foreach (var index in songs.Select(x => int.Parse(x)))
            {
                list.Tracks.RemoveAt(index + 1);
            }

            int curState = list.Tracks.Count;

            ConfigManager.Save();

            await ctx.RespondAsync(new DiscordEmbedBuilder()
                .WithAuthor($"Removed {curState - list.Tracks.Count} song(s) from {list.ID}!")
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
                .AddField("Version", $"Polaris ({Program.Version}-{Program.Version.Release.Name.ToLower()})\nGateway (v{ctx.Client.GatewayVersion})", true)
                .AddField("Library", $"DSharpPlus ({ctx.Client.VersionString})\nNiveraLib ({LibProperties.LibraryVersion})", true)
                .AddField("Ping", $"{ctx.Client.Ping} ms", true)
                .AddField("Voice Gateway Ping", $"{CoreCollection.ActiveCores.Select(x => x.ServerMusicCore.radioModule).FirstOrDefault(x => x.IsConnected)?.WebPing ?? -1} ms", true)
                .AddField("Core ID", $"P-{cores.ServerCore.CoreId}", true)
                .AddField("Owner", $"<@!{GlobalConfig.Instance.BotOwnerId}>", true)
                .AddField("Uptime", TimeSpan.FromMilliseconds(Program.UptimeSeconds).ToString(), true)
                .WithTimestamp(DateTimeOffset.Now.ToLocalTime())
                .WithFooter($"© {Resources.Developer}, 2022"));
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

        [Command("join")]
        [RequireGuild]
        public async Task Join(CommandContext ctx, string type = null)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("A song is currently playing.")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Join(ctx.Member.VoiceState.Channel, ctx.Channel, ctx.Message.Content.Contains("-radio") ? "radio" : "lavalink");
        }

        [Command("leave")]
        [Aliases("dis", "disconnect")]
        [RequireGuild]
        public async Task Leave(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (!coreCollection.ServerMusicCore.IsConnected)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("I am not in a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel == null || coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Disconnect();
        }

        [Command("play")]
        [Aliases("pl")]
        [RequireGuild]
        public async Task Play(CommandContext ctx, [RemainingText] string url)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (coreCollection.ServerMusicCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
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

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            CommandExtensions.DeleteMusic.Enqueue(await ctx.Channel.SendMessageAsync($"**{EmotePicker.LoadingGif} Loading track(s) ..**"));

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Play(url, ctx.Member ?? ctx.User);
        }

        [Command("pause")]
        [Aliases("ps")]
        [RequireGuild]
        public async Task Pause(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Pause();
        }

        [Command("resume")]
        [Aliases("res")]
        [RequireGuild]
        public async Task Resume(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Resume();
        }

        [Command("nowplaying")]
        [Aliases("np")]
        [RequireGuild]
        public async Task NowPlaying(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.NowPlaying();
        }

        [Command("seek")]
        [RequireGuild]
        public async Task Seek(CommandContext ctx, TimeSpan time)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Seek(time);
        }

        [Command("playpartial")]
        [Aliases("ppl")]
        [RequireGuild]
        public async Task PlayPartial(CommandContext ctx, TimeSpan from, TimeSpan to)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.PlayPartial(from, to);
        }

        [Command("queue")]
        [Aliases("q")]
        [RequireGuild]
        public async Task Queue(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.ViewQueue(ctx.User);
        }

        [Command("clearqueue")]
        [Aliases("cq")]
        [RequireGuild]
        public async Task ClearQueue(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.ClearQueue();
        }

        [Command("loop")]
        [RequireGuild]
        public async Task Loop(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);

            if (coreCollection.ServerMusicCore.IsLooping)
            {
                coreCollection.ServerMusicCore.IsLooping = false;

                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Looping disabled.")
                    .WithColor(ColorPicker.SuccessColor)
                    .AddEmoteAuthor(EmotePicker.RepeatEmote));
            }
            else
            {
                coreCollection.ServerMusicCore.IsLooping = true;

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

            if (ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not connected to a voice channel!")
                    .MakeError());

                return;
            }

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Stop();
        }

        [Command("volume")]
        [Aliases("vol")]
        [RequireGuild]
        public async Task Volume(CommandContext ctx, int volume)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (coreCollection.ServerMusicCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
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

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            await coreCollection.ServerMusicCore.lavalinkModule.SetVolumeAsync(volume);
        }

        [Command("skip")]
        [Aliases("s")]
        [RequireGuild]
        public async Task Skip(CommandContext ctx)
        {
            CoreCollection coreCollection = CoreCollection.Get(ctx);

            if (coreCollection.ServerMusicCore.IsPlaying && !ctx.CheckPerms("dj", out coreCollection))
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

            if (coreCollection.ServerMusicCore.AudioChannel != null && (coreCollection.ServerMusicCore.AudioChannel.Id != ctx.Member.VoiceState.Channel.Id))
            {
                await ctx.RespondAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You are not in the same voice channel!")
                    .MakeError());

                return;
            }

            coreCollection.ServerMusicCore.SetChannels(ctx.Channel, ctx.Member.VoiceState.Channel);
            coreCollection.ServerMusicCore.Skip();
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
                Log.SendError(ex);
            }
        }

    }
}
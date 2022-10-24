using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.VoiceNext;

using NiveraLib.Logging;
using NiveraLib;

using Polaris.Config;
using Polaris.Discord;
using Polaris.Entities;
using Polaris.Helpers;
using Polaris.Pagination;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Polaris.Helpers.Music;

namespace Polaris.Core.MusicModules
{
    public class LavalinkMusicModule
    {
        private bool isPlaying;
        private LogId logId;
        private ServerMusicCore core;

        public const string TimeSpanFormat = "hh:mm:ss";

        private string IP;
        private string Passwd;

        private int Port;
        private int volume;
        private bool paused;

        private LavalinkExtension Lavalink;
        private LavalinkNodeConnection LavalinkNode;
        private LavalinkGuildConnection LavalinkGuild;
        private LavalinkTrack LavalinkTrack;

        private DiscordChannel TextChannel;
        private DiscordChannel VoiceChannel;

        public ConcurrentQueue<LavalinkTrack> Queue = new ConcurrentQueue<LavalinkTrack>();

        public int Volume
        {
            get => volume;
            set
            {
                if (LavalinkGuild != null)
                {
                    LavalinkGuild.SetVolumeAsync(value);

                    volume = value;
                }
            }
        }

        public bool IsPaused
        {
            get => paused;
            set
            {
                if (value)
                    Task.Run(async () =>
                    {
                        await LavalinkGuild?.PauseAsync();
                    });
                else
                    Task.Run(async () =>
                    {
                        await LavalinkGuild?.ResumeAsync();
                    });

                paused = value;
            }
        }

        public bool IsConnected
        {
            get
            {
                return LavalinkGuild != null && LavalinkGuild.IsConnected;
            }
        }

        public bool IsServerConnected
        {
            get
            {
                return LavalinkNode != null && LavalinkNode.IsConnected;
            }
        }

        public bool IsPlaying
        {
            get => isPlaying;
        }

        public bool IsLooping { get; set; }

        public LavalinkTrack Track
        {
            get => LavalinkGuild?.CurrentState?.CurrentTrack;
        }

        public DiscordChannel Voice
        {
            get => LavalinkGuild?.Channel;
        }

        public DiscordChannel Text
        {
            get => TextChannel;
            set => TextChannel = value;
        }

        public LavalinkMusicModule(ServerMusicCore core, ulong guildId)
        {
            this.core = core;

            logId = new LogId("modules / lavalink", (long)guildId);

            Log.SendInfo($"Loading ..", logId);

            Lavalink = DiscordNetworkHandlers.LavalinkExtension;
            Lavalink.NodeDisconnected += Lavalink_NodeDisconnected;

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                Log.SendWarn("The Lavalink core is disabled in the config. Launch Polaris with the \"-globalconfig:allowlava=true\" argument to enable it.", logId);

                return;
            }

            if (Lavalink.ConnectedNodes.Count < 1)
            {
                string ip = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkAddress);

                try
                {
                    IP = ip.Split(':')[0];
                    Port = int.Parse(ip.Split(':')[1]);
                }
                catch
                {
                    Log.SendError("Incorrect Lavalink IP format - setting to default (127.0.0.1:2333), make sure your IP and Port are split by a : (IP:Port)", logId);

                    IP = "127.0.0.1";
                    Port = 2333;
                }

                Passwd = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword);

                Task.Run(async () => await RunAsync());
            }
            else
            {
                Log.SendInfo($"Trying to connect to a node ..", logId);

                LavalinkNode = Lavalink.ConnectedNodes.FirstOrDefault().Value;

                if (LavalinkNode != null)
                {
                    InstallHandlers(LavalinkNode);

                    Log.SendInfo($"Succesfully connected to a node: {LavalinkNode.NodeEndpoint}", logId);
                }
            }
        }

        private Task Lavalink_NodeDisconnected(LavalinkNodeConnection sender, NodeDisconnectedEventArgs e)
        {
            Log.SendWarn($"Node {e.LavalinkNode.NodeEndpoint} has disconnected. Reason: {(e.IsCleanClose ? "Native Exit" : "SendError")}", logId);

            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            try
            {
                Log.SendInfo($"Trying to connect to a node ..");

                LavalinkNode = await Lavalink.ConnectAsync(new LavalinkConfiguration
                {
                    Password = Passwd,

                    RestEndpoint = new ConnectionEndpoint
                    {
                        Hostname = IP,
                        Port = Port,
                        Secured = false
                    },

                    SocketEndpoint = new ConnectionEndpoint
                    {
                        Hostname = IP,
                        Port = Port,
                        Secured = false
                    }
                });

                InstallHandlers(LavalinkNode);

                Log.SendInfo($"Succesfully connected to a node: {LavalinkNode.NodeEndpoint}", logId);
            }
            catch (Exception ex)
            {
                Log.SendError($"Failed to connect to the lavalink node - make sure it's running on {IP}:{Port} and your password is correct ({Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword)}", logId);
                Log.SendError(ex);
            }
        }

        private void InstallHandlers(LavalinkNodeConnection lavalinkNodeConnection)
        {
            lavalinkNodeConnection.GuildConnectionCreated += (x, e) =>
            {
                if (x.Guild.Id != (ulong)logId.Id)
                    return Task.CompletedTask;

                Log.SendTrace($"Connected to guild: {x.Guild.Name}", logId);

                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Joined {x.Channel.Name}")
                    .MakeSuccess());

                VoiceChannel = x.Channel;
                LavalinkGuild = x;

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.GuildConnectionRemoved += (x, e) =>
            {
                if (x.Guild.Id != (ulong)logId.Id)
                    return Task.CompletedTask;

                Log.SendTrace($"Disconnected from guild: {x.Guild.Name}", logId);

                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Left {x.Channel.Name}")
                    .AddEmoteAuthor(EmotePicker.WaveHandEmote)
                    .WithColor(ColorPicker.InfoColor));

                TextChannel = null;
                VoiceChannel = null;
                LavalinkGuild = null;
                LavalinkTrack = null;

                IsLooping = false;
                isPlaying = false;

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.LavalinkSocketErrored += (x, e) =>
            {
                Log.SendError($"An error occured while connecting!");
                Log.SendError(e.Exception);

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.PlaybackFinished += async (x, e) =>
            {
                if (x.Guild.Id != (ulong)logId.Id)
                    return;

                Log.SendTrace($"Finished playing {e.Track.Title} in {x.Guild.Name}: {e.Reason}");

                isPlaying = false;

                if (IsLooping)
                {
                    await LavalinkGuild.PlayAsync(LavalinkTrack);
                    await NowPlaying();
                }
                else
                {
                    LavalinkTrack = null;
                }

                if (Queue.TryDequeue(out LavalinkTrack))
                {
                    await LavalinkGuild.PlayAsync(LavalinkTrack);
                    await NowPlaying();
                }
            };

            lavalinkNodeConnection.PlaybackStarted += (x, e) =>
            {
                if (x.Guild.Id != (ulong)logId.Id)
                    return Task.CompletedTask;

                isPlaying = true;

                Log.SendTrace($"Started playing {e.Track.Title} in {x.Guild.Name}");

                LavalinkGuild = e.Player;
                LavalinkTrack = e.Track;

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.TrackException += (x, e) =>
            {
                if (x.Guild.Id != (ulong)logId.Id)
                    return Task.CompletedTask;

                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback")
                    .WithTitle("An error occured while playing this track.")
                    .WithFooter(e.Error)
                    .MakeError());

                isPlaying = false;

                return Task.CompletedTask;
            };
        }

        public async Task<List<LavalinkTrack>> ConvertPlaylist(UserPlaylist userPlaylist)
        {
            var msg = await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithDescription("Converting your playlist ..")
                .AddEmoteDescription(EmotePicker.LoadingGif)
                .WithColor(ColorPicker.SuccessColor));

            List<LavalinkTrack> lavalinkTracks = new List<LavalinkTrack>();

            foreach (var musicTrack in userPlaylist.Tracks)
            {
                var lavaTrack = await FindTrack(musicTrack.Title);

                if (lavaTrack == null)
                {
                    await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Playback")
                        .WithTitle($"Failed to load track, skipping: {musicTrack.Title}.")
                        .MakeWarn());

                    continue;
                }
                else
                {
                    lavalinkTracks.Add(lavaTrack);
                }
            }

            if (msg != null)
                await msg.DeleteAsync();

            return lavalinkTracks;
        }

        public async Task<LavalinkTrack> FindTrack(string search)
        {
            IResult searchRes = await MusicSearch.Search(search);

            if (searchRes is ErrorSearchResult)
                return null;

            return await LoadLavalink(searchRes.SelectedTrack);
        }

        public async Task JoinAsync(DiscordChannel voiceChannel, DiscordChannel textChannel)
        {
            TextChannel = textChannel;
            VoiceChannel = voiceChannel;

            if (IsConnected && LavalinkGuild.Channel.Id != VoiceChannel.Id)
                await LeaveAsync();

            await voiceChannel.ConnectAsync(LavalinkNode);

            while (LavalinkGuild == null)
            {
                await Task.Delay(100);

                continue;
            }
        }

        public async Task SetVolumeAsync(int volume)
        {
            if (volume > 100)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Volume")
                    .WithTitle("You cannot set the volume higher than 100%!")
                    .MakeError());

                return;
            }

            if (volume < 0)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Volume")
                    .WithTitle("You cannot set the volume lower than 0%!")
                    .MakeError());

                return;
            }

            Volume = volume;

            string emote = EmbedHelper.VolumeEmoteToUse(volume);

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithTitle($"{emote} Volume set to {volume}%")
                .WithColor(ColorPicker.InfoColor));
        }

        public async Task PauseAsync()
        {
            if (IsPaused)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback")
                    .WithTitle("This track is already paused!")
                    .MakeWarn());

                return;
            }

            IsPaused = true;

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Paused!")
                .AddEmoteAuthor(EmotePicker.PauseEmote)
                .WithColor(ColorPicker.InfoColor));
        }

        public async Task ResumeAsync()
        {
            if (!IsPaused)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback")
                    .WithTitle("This track is not paused!")
                    .MakeWarn());

                return;
            }

            IsPaused = false;

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Resumed!")
                .AddEmoteAuthor(EmotePicker.PlayEmote)
                .WithColor(ColorPicker.InfoColor));
        }

        public async Task NowPlaying()
        {
            var track = Track;

            if (track == null)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing playing.")
                    .MakeInfo());

                return;
            }

            TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Now Playing")
                .AddField("Title", $"[{track.Title}]({track.Uri.AbsoluteUri})", true)
                .AddField("Author", track.Author, true)
                .AddField("Duration", track.Length.ToString(), true)
                .AddField("Position", LavalinkGuild.CurrentState.PlaybackPosition.ToString(), true)
                .AddEmoteAuthor(EmotePicker.PlayEmote)
                .WithColor(ColorPicker.InfoColor));
        }

        public async Task SeekAsync(TimeSpan time)
        {
            if (Track == null)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing playing.")
                    .MakeWarn());

                return;
            }

            var current = LavalinkGuild.CurrentState.PlaybackPosition;

            await LavalinkGuild.PauseAsync();
            await LavalinkGuild.SeekAsync(time);
            await LavalinkGuild.ResumeAsync();

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor($"Started playing from {time}")
                .WithColor(ColorPicker.InfoColor)
                .AddEmoteAuthor(time > current ? EmotePicker.TrackForwardEmote : EmotePicker.TrackReverseEmote));
        }

        public async Task PlayPartial(TimeSpan from, TimeSpan to)
        {
            if (Track == null)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing playing.")
                    .MakeWarn());

                return;
            }

            var track = Track;

            await LavalinkGuild.StopAsync();
            await LavalinkGuild.PlayPartialAsync(track, from, to);
        }

        public async Task PlayAsync(UserPlaylist userPlaylist)
        {
            await JoinAsync(VoiceChannel, TextChannel);
            await PlayAsync(await ConvertPlaylist(userPlaylist));
        }

        public async Task PlayAsync(List<LavalinkTrack> tracks)
        {
            if (tracks == null || tracks.Count < 1)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing in the playlist.")
                    .MakeError());

                return;
            }

            await JoinAsync(VoiceChannel, TextChannel);

            Queue.Clear();

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Queue cleared, playing your playlist.")
                .MakeInfo());

            var toPlay = tracks.First();

            tracks.RemoveAt(0);

            foreach (var track in tracks)
                Queue.Enqueue(track);

            await LavalinkGuild.PlayAsync(toPlay);
        }

        public async Task SkipAsync()
        {
            if (!Queue.TryDequeue(out var nextTrack))
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing in the queue, stopping playback.")
                    .MakeWarn());

                IsLooping = false;

                await LavalinkGuild.StopAsync();

                return;
            }

            bool loop = IsLooping;

            IsLooping = false;

            await LavalinkGuild.StopAsync();
            await LavalinkGuild.PlayAsync(nextTrack);

            await NowPlaying();

            IsLooping = loop;
        }

        public async Task PlayAsync(string searchOrUrl, DiscordChannel voice, DiscordChannel text, DiscordUser author = null)
        {
            try
            {
                VoiceChannel = voice;
                TextChannel = text;

                await JoinAsync(VoiceChannel, TextChannel);

                IResult searchResult = await MusicSearch.Search(searchOrUrl);

                while (CommandExtensions.DeleteMusic.TryDequeue(out var msg))
                    await msg.DeleteAsync();

                if (searchResult is ErrorSearchResult error)
                {
                    await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Search Failed")
                        .WithTitle(error.Reason)
                        .MakeError());

                    return;
                }

                if (searchResult is PlaylistSearchResult playlist)
                {
                    Log.SendTrace("Received PlaylistSearchResult");
                    Log.SendJson(playlist);

                    var music = playlist.SelectedTrack;
                    var lavaTrack = await LoadLavalink(music);
                    var loadedTracks = new List<LavalinkTrack>();

                    foreach (var track in playlist.OtherTracks)
                    {
                        Log.SendTrace($"Converting track: {track.Title}");

                        var lava = await LoadLavalink(track);

                        if (lava != null)
                        {
                            Log.SendTrace($"Converted track: {lava.Title}");

                            loadedTracks.Add(lava);
                        }
                        else
                        {
                            Log.SendError("Failed to convert track.");
                        }
                    }

                    await TextChannel?.SendPaginatedMessageAsync(author, PageParser.SplitToPages(loadedTracks, new DiscordEmbedBuilder()
                        .WithTitle($"{playlist.Name} ({playlist.OtherTracks.Count + 1}).")
                        .WithDescription($"Playing from: [{lavaTrack.Title}]({lavaTrack.Uri.AbsoluteUri})")
                        .WithFooter($"By: [{searchResult.SelectedTrack.Author}]({searchResult.SelectedTrack.AuthorUrl})")
                        .WithThumbnail(playlist.ThumbnailUrl)
                        .AddEmoteTitle(EmotePicker.PlayEmote)
                        .WithColor(ColorPicker.SuccessColor)), PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);

                    if (IsPlaying)
                    {
                        TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                            .WithTitle("There's a song currently playing, request queued.")
                            .AddField("Title", $"[{lavaTrack.Title}]({lavaTrack.Uri.AbsoluteUri})", true)
                            .AddField("Duration", searchResult.SelectedTrack.Duration.ToString(), true)
                            .WithFooter($"By: [{searchResult.SelectedTrack.Author}]({searchResult.SelectedTrack.AuthorUrl})")
                            .WithThumbnail(searchResult.SelectedTrack.Thumbnail)
                            .AddEmoteTitle(EmotePicker.PlayEmote)
                            .WithColor(ColorPicker.SuccessColor));

                        Queue.Enqueue(lavaTrack);

                        foreach (var trrr in loadedTracks.Where(x => x.Identifier != lavaTrack.Identifier))
                            Queue.Enqueue(trrr);

                        return;
                    }
                    else
                    {
                        TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                                .WithTitle("Now Playing")
                                .AddField("Title", $"[{lavaTrack.Title}]({lavaTrack.Uri.AbsoluteUri})", true)
                                .AddField("Duration", searchResult.SelectedTrack.Duration.ToString(), true)
                                .WithFooter($"By: [{searchResult.SelectedTrack.Author}]({searchResult.SelectedTrack.AuthorUrl})")
                                .WithThumbnail(searchResult.SelectedTrack.Thumbnail)
                                .AddEmoteTitle(EmotePicker.PlayEmote)
                                .WithColor(ColorPicker.SuccessColor));

                        LavalinkTrack = lavaTrack;

                        foreach (var trrr in loadedTracks.Where(x => x.Identifier != lavaTrack.Identifier))
                            Queue.Enqueue(trrr);

                        await LavalinkGuild.PlayAsync(lavaTrack);
                    }

                    return;
                }

                Log.SendTrace("Received other search result.");
                Log.SendJson(searchResult);

                var lavaTrackToPlay = await LoadLavalink(searchResult.SelectedTrack);

                if (lavaTrackToPlay == null)
                {
                    await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Playback Failed")
                        .WithTitle("Failed to select the track to play.")
                        .MakeError());

                    return;
                }

                if (IsPlaying)
                {
                    TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithTitle("There's a song currently playing, request queued.")
                        .AddField("Title", $"[{lavaTrackToPlay.Title}]({lavaTrackToPlay.Uri.AbsoluteUri})", true)
                        .AddField("Duration", searchResult.SelectedTrack.Duration.ToString(), true)
                        .WithFooter($"By: [{searchResult.SelectedTrack.Author}]({searchResult.SelectedTrack.AuthorUrl})")
                        .WithThumbnail(searchResult.SelectedTrack.Thumbnail)
                        .AddEmoteTitle(EmotePicker.PlayEmote)
                        .WithColor(ColorPicker.SuccessColor));

                    Queue.Enqueue(lavaTrackToPlay);

                    return;
                }

                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithTitle("Now Playing")
                        .AddField("Title", $"[{lavaTrackToPlay.Title}]({lavaTrackToPlay.Uri.AbsoluteUri})", true)
                        .AddField("Duration", searchResult.SelectedTrack.Duration.ToString(), true)
                        .WithFooter($"By: [{searchResult.SelectedTrack.Author}]({searchResult.SelectedTrack.AuthorUrl})")
                        .WithThumbnail(searchResult.SelectedTrack.Thumbnail)
                        .AddEmoteTitle(EmotePicker.PlayEmote)
                        .WithColor(ColorPicker.SuccessColor));

                LavalinkTrack = lavaTrackToPlay;

                await LavalinkGuild.PlayAsync(lavaTrackToPlay);
            }
            catch (Exception ex)
            {
                Log.SendException(ex);
            }
        }

        public async Task ViewQueueAsync(DiscordUser author)
        {
            if (Queue.Count < 1)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing in the queue.")
                    .MakeInfo());

                return;
            }

            await TextChannel?.SendPaginatedMessageAsync(author, PageParser.SplitToPages(Queue.ToList(), new DiscordEmbedBuilder()
                .WithAuthor($"Your queue ({Queue.Count}):")
                .WithColor(ColorPicker.InfoColor)
                .AddEmoteAuthor(EmotePicker.CycloneEmote)), PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);
        }

        public async Task ClearQueueAsync()
        {
            Queue.Clear();

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Queue cleared!")
                .AddEmoteAuthor(EmotePicker.PopEmote)
                .WithColor(ColorPicker.InfoColor));
        }

        public async Task<LavalinkTrack> LoadLavalink(IMusicTrack musicTrack)
        {
            var result = await LavalinkGuild.GetTracksAsync(musicTrack.Url, LavalinkSearchType.Youtube);

            if (result.LoadResultType == LavalinkLoadResultType.LoadFailed || result.LoadResultType == LavalinkLoadResultType.NoMatches)
                return null;

            return result.Tracks.FirstOrDefault();
        }

        public async Task StopAsync()
        {
            if (Track == null)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing playing.")
                    .MakeWarn());

                return;
            }

            await LavalinkGuild.StopAsync();

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Stopped the playback.")
                .MakeInfo());
        }

        public async Task LeaveAsync()
        {
            await LavalinkGuild.DisconnectAsync(true);
        }

        public void SetTextChannel(DiscordChannel discordChannel)
        {
            TextChannel = discordChannel;
        }

        public void SetVoiceChannel(DiscordChannel discordChannel)
        {
            VoiceChannel = discordChannel;
        }
    }
}
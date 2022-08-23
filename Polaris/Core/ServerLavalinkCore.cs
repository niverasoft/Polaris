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

using Polaris.Pagination;
using Polaris.Config;
using Polaris.Boot;
using Polaris.Core;
using Polaris.Discord;
using Polaris.Entities;
using Polaris.Enums;
using Polaris.Helpers;
using Polaris.Properties;

using Nivera;

namespace Polaris.Core
{
    public class ServerLavalinkCore
    {
        public const string TimeSpanFormat = "hh:mm:ss";

        private string IP;
        private string Passwd;

        private int Port;
        private int volume;
        private bool paused;

        private LavalinkExtension Lavalink;
        private LavalinkNodeConnection LavalinkNode;
        private LavalinkGuildConnection LavalinkGuild;
        private DiscordChannel TextChannel;
        private DiscordChannel VoiceChannel;
        private LavalinkTrack LavalinkTrack;

        public List<LavalinkTrack> Queue = new List<LavalinkTrack>();

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
            get => LavalinkGuild?.CurrentState != null && Track != null;
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
        }

        public ServerLavalinkCore()
        {
            Log.JoinCategory("cores/lavalink");

            Lavalink = DiscordNetworkHandlers.LavalinkExtension;
            Lavalink.NodeDisconnected += Lavalink_NodeDisconnected;

            if (!GlobalConfig.Instance.AllowLavalink)
            {
                Log.Warn("The Lavalink core is disabled in the config. Launch Polaris with the \"-globalconfig:allowlava=true\" argument to enable it.");
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
                    Log.Error("Incorrect Lavalink IP format - setting to default (127.0.0.1:2333), make sure your IP and Port are split by a : (IP:Port)");

                    IP = "127.0.0.1";
                    Port = 2333;
                }

                Passwd = Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword);

                Task.Run(async () => await RunAsync());
            }
            else
            {
                Log.Info($"Trying to connect to a node ..");

                LavalinkNode = Lavalink.ConnectedNodes.FirstOrDefault().Value;

                if (LavalinkNode != null)
                {
                    InstallHandlers(LavalinkNode);

                    Log.Info($"Succesfully connected to a node: {LavalinkNode.NodeEndpoint}");
                }
            }
        }

        private Task Lavalink_NodeDisconnected(LavalinkNodeConnection sender, NodeDisconnectedEventArgs e)
        {
            Log.Warn($"Node {e.LavalinkNode.NodeEndpoint} has disconnected. Reason: {(e.IsCleanClose ? "Native Exit" : "Error")}");

            return Task.CompletedTask;
        }

        public async Task RunAsync()
        {
            try
            {
                Log.Info($"Trying to connect to a node ..");

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

                Log.Info($"Succesfully connected to a node: {LavalinkNode.NodeEndpoint}");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to connect to the lavalink node - make sure it's running on {IP}:{Port} and your password is correct ({Encoding.UTF32.GetString(GlobalConfig.Instance.LavalinkPassword)}");
                Log.Error(ex);
            }
        }

        private void InstallHandlers(LavalinkNodeConnection lavalinkNodeConnection)
        {
            lavalinkNodeConnection.GuildConnectionCreated += (x, e) =>
            {
                Log.Verbose($"Connected to guild: {x.CurrentState} <-> {x.Guild.Name}");

                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Joined {x.Channel.Name}")
                    .MakeSuccess());

                VoiceChannel = x.Channel;
                LavalinkGuild = x;

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.GuildConnectionRemoved += (x, e) =>
            {
                Log.Verbose($"Disconnected from guild: {x.Guild.Name}");

                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Left {x.Channel.Name}")
                    .AddEmoteAuthor(EmotePicker.WaveHandEmote)
                    .WithColor(ColorPicker.InfoColor));

                VoiceChannel = null;
                TextChannel = null;
                LavalinkGuild = null;
                LavalinkTrack = null;

                IsLooping = false;

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.LavalinkSocketErrored += (x, e) =>
            {
                Log.Error($"An error occured while connecting!");
                Log.Error(e.Exception);

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.PlaybackFinished += async (x, e) =>
            {
                Log.Verbose($"Finished playing {e.Track.Title} in {x.Guild.Name}: {e.Reason}");

                if (IsLooping)
                {
                    await LavalinkGuild.PlayAsync(LavalinkTrack);
                    await NowPlaying();
                }
                else
                {
                    LavalinkTrack = null;
                }

                if (Queue.Count > 0)
                {
                    await LavalinkGuild.PlayAsync(Queue.First());
                    await NowPlaying();

                    Queue.RemoveAt(0);
                }
            };

            lavalinkNodeConnection.PlaybackStarted += (x, e) =>
            {
                Log.Verbose($"Started playing {e.Track.Title} in {x.Guild.Name}");

                LavalinkGuild = e.Player;
                LavalinkTrack = e.Track;

                return Task.CompletedTask;
            };

            lavalinkNodeConnection.TrackException += (x, e) =>
            {
                TextChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback Error")
                    .WithTitle("An error occured while playing this track.")
                    .WithFooter(e.Error)
                    .MakeError());

                return Task.CompletedTask;
            };
        }

        public async Task<List<LavalinkTrack>> ConvertPlaylist(UserPlaylist userPlaylist)
        {
            List<LavalinkTrack> lavalinkTracks = new List<LavalinkTrack>();

            foreach (var musicTrack in userPlaylist.Tracks)
            {
                var lavaTrack = await FindTrack(musicTrack.URL);

                if (lavaTrack == null)
                {
                    await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Playback Error")
                        .WithTitle($"Failed to load track, skipping: [{musicTrack.Title}]({musicTrack.URL})")
                        .MakeWarn());

                    continue;
                }
                else
                {
                    lavalinkTracks.Add(lavaTrack);
                }
            }

            return lavalinkTracks;
        }

        public async Task<LavalinkTrack> FindTrack(string search)
        {
            LavalinkLoadResult res = await LavalinkGuild.GetTracksAsync(search, LavalinkSearchType.Youtube);

            if (res.LoadResultType == LavalinkLoadResultType.TrackLoaded || res.LoadResultType == LavalinkLoadResultType.SearchResult)
                return res.Tracks.First();

            if (res.LoadResultType == LavalinkLoadResultType.PlaylistLoaded || res.LoadResultType == LavalinkLoadResultType.NoMatches)
                return null;

            return null;
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
                    .WithAuthor($"Volume Error")
                    .WithTitle("You cannot set the volume higher than 100%!")
                    .MakeError());

                return;
            }

            if (volume < 0)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Volume Error")
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
                    .WithAuthor("Playback Error")
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
                    .WithAuthor("Playback Error")
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
                .WithAuthor($"Started playing from {time.ToString()}")
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
            await JoinAsync(VoiceChannel, TextChannel);

            Queue.Clear();

            await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Queue cleared, playing your playlist.")
                .MakeInfo());

            Queue.AddRange(tracks);

            await LavalinkGuild.PlayAsync(Queue.First());

            Queue.RemoveAt(0);
        }

        public async Task SkipAsync()
        {
            if (Queue.Count < 1)
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
            await LavalinkGuild.PlayAsync(Queue.First());

            Queue.RemoveAt(0);

            await NowPlaying();

            IsLooping = loop;
        }

        public async Task PlayAsync(string searchOrUrl, DiscordChannel voice, DiscordChannel text, DiscordUser author = null)
        {
            VoiceChannel = voice;
            TextChannel = text;

            await JoinAsync(VoiceChannel, TextChannel);

            LavalinkLoadResult res = await LavalinkGuild.GetTracksAsync(searchOrUrl);

            if (res.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder().WithAuthor("Search Failed");

                if (!string.IsNullOrEmpty(res.Exception.Message))
                    builder.WithTitle(res.Exception.Message);

                builder.MakeError();

                await TextChannel?.SendMessageAsync(builder);

                return;
            }

            if (res.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Search Failed")
                    .WithTitle("No tracks match your search.")
                    .MakeError());

                return;
            }

            LavalinkTrack trackToPlay = null;

            if (res.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
            {
                trackToPlay = res.Tracks.ElementAtOrDefault(res.PlaylistInfo.SelectedTrack);

                await TextChannel?.SendPaginatedMessageAsync(author, PageParser.SplitToPages(res.Tracks.ToList(), new DiscordEmbedBuilder()
                    .WithAuthor($"Playlist loaded: {res.PlaylistInfo.Name} ({res.Tracks.Count()}).")
                    .WithTitle($"Playing from: [{trackToPlay.Title}]({trackToPlay.Uri.AbsoluteUri})")
                    .MakeInfo()), PaginationBehaviour.WrapAround, ButtonPaginationBehavior.Disable);

                if (IsPlaying)
                    Queue.Add(trackToPlay);

                Queue.AddRange(res.Tracks.Where(x => x.Identifier != trackToPlay.Identifier));
            }
            else
            {
                trackToPlay = res.Tracks.First();
            }

            if (IsPlaying)
            {
                TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There's a song currently playing, request queued.")
                    .AddField("Title", $"[{trackToPlay.Title}]({trackToPlay.Uri.AbsoluteUri})", true)
                    .AddField("Author", trackToPlay.Author, true)
                    .AddField("Duration", trackToPlay.Length.ToString(), true)
                    .AddField("Position", LavalinkGuild.CurrentState.PlaybackPosition.ToString(), true)
                    .AddEmoteAuthor(EmotePicker.PlayEmote)
                    .WithColor(ColorPicker.InfoColor));

                Queue.Add(trackToPlay);

                return;
            }

            if (trackToPlay == null)
            {
                await TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback Failed")
                    .WithTitle("Failed to select the track to play.")
                    .MakeError());

                return;
            }

            TextChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Now Playing")
                .AddField("Title", $"[{trackToPlay.Title}]({trackToPlay.Uri.AbsoluteUri})", true)
                .AddField("Author", trackToPlay.Author, true)
                .AddField("Duration", trackToPlay.Length.ToString(), true)
                .AddField("Position", LavalinkGuild.CurrentState.PlaybackPosition.ToString(), true)
                .AddEmoteAuthor(EmotePicker.PlayEmote)
                .WithColor(ColorPicker.InfoColor));

            LavalinkTrack = trackToPlay;

            await LavalinkGuild.PlayAsync(trackToPlay);
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

            await TextChannel?.SendPaginatedMessageAsync(author, PageParser.SplitToPages(Queue, new DiscordEmbedBuilder()
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

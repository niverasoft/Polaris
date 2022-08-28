using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.VoiceNext;

using Polaris.Config;
using Polaris.Discord;
using Polaris.Entities;
using Polaris.Helpers;

using Nivera;
using System;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Polaris.Core
{
    public class ServerRadioCore
    {
        private Thread _ffmpegThread;
        private ulong _serverId;

        private volatile Process _ffmpegProcess;
        private volatile Stream _ffmpegStream;
        private volatile DiscordChannel _voiceChannel;
        private volatile DiscordChannel _textChannel;
        private volatile RadioStation _radioStation;
        private volatile VoiceNextConnection _voiceNextConn;
        private volatile VoiceNextExtension _voiceNext;
        private volatile VoiceTransmitSink _voiceSink;
        private CancellationTokenSource _tokenSource;
        private volatile bool _voicePause;

        public bool IsActive { get => !_tokenSource.IsCancellationRequested; }
        public bool IsConnected { get => _voiceNextConn != null && _voiceNextConn.TargetChannel != null; }
        public bool IsPaused { get => _voicePause; }
        public bool IsPlaying { get => _voiceNextConn?.IsPlaying ?? false; }
        public bool AllowVoiceCapture { get; }
        public int WebPing { get => _voiceNextConn?.WebSocketPing ?? 0; }
        public int UdpPing { get => _voiceNextConn?.UdpPing ?? 0; }

        public ServerRadioCore(ulong serverId)
        {
            _voiceNext = DiscordNetworkHandlers.GlobalClient.GetVoiceNext();
            _serverId = serverId;
            AllowVoiceCapture = CoreCollection.Get(_serverId)?.ServerConfig.AllowVoiceCapture ?? false;
        }

        public void InstallHandlers(VoiceNextConnection voiceNextConnection)
        {
            voiceNextConnection.VoiceSocketErrored += (x, e) =>
            {
                Log.Error($"An error occured in the VoiceNext WebSocket");
                Log.Arguments(x, e);

                return Task.CompletedTask;
            };

            voiceNextConnection.VoiceReceived += (x, e) =>
            {
                if (AllowVoiceCapture && e.User != null)
                {
                    VoiceCache voiceCache = ConfigManager.VoiceList.TryGetValue(e.User.Id, out var cache) ? cache : new VoiceCache();

                    voiceCache.DiscordUserId = e.User.Id;
                    voiceCache.LastSSRC = e.SSRC;
                    voiceCache.OpusVoicePackets.Add(e.OpusData.ToArray());
                    voiceCache.PcmVoicePackets.Add(e.PcmData.ToArray());

                    Log.Info("VoiceCapture ->");
                    Log.Arguments(e);

                    ConfigManager.VoiceList[e.User.Id] = voiceCache;
                }

                return Task.CompletedTask;
            };
        }

        public async Task JoinAsync(DiscordChannel voice, DiscordChannel text)
        {
            if (IsConnected)
            {
                if (_voiceNextConn.TargetChannel.Id == voice.Id)
                    return;

                await DisconnectAsync();
            }

            _voiceChannel = voice;
            _textChannel = text;

            _voiceNextConn = await voice.ConnectAsync();
            _voiceSink = _voiceNextConn.GetTransmitSink();

            await text.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor($"Joined {voice.Name}!")
                .MakeSuccess());
        }

        public async Task NowPlayingAsync()
        {
            if (_textChannel == null)
                return;

            if (!IsPlaying)
            {
                await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("There is nothing playing.")
                    .MakeError());

                return;
            }

            if (string.IsNullOrEmpty(_radioStation?.DataUrl))
            {
                await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("You have to set the station's data URL in order to use this command.")
                    .MakeError());

                return;
            }

            await _textChannel.SendMessageAsync(await AddStationInfo(_radioStation, new DiscordEmbedBuilder()
                .WithAuthor("Now Playing")
                .AddEmoteAuthor(EmotePicker.NotesEmote)
                .WithColor(ColorPicker.SuccessColor)));
        }

        public async Task DisconnectAsync()
        {
            try
            {
                await StopAsync();

                _voiceNextConn.Disconnect();
                _voiceNextConn.Dispose();
                _voiceNextConn = null;
                _voicePause = false;

                ConfigManager.Save();

                await _textChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor($"Disconnected from {_voiceChannel.Name}!")
                    .MakeSuccess());

                _textChannel = null;
                _voiceChannel = null;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public async Task PauseAsync()
        {
            if (_textChannel != null)
            {
                if (!IsConnected)
                {
                    await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("I am not connected.")
                        .MakeError());

                    return;
                }

                if (IsPaused)
                {
                    await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Playback is already paused.")
                        .MakeError());

                    return;
                }

                _voiceNextConn.Pause();
                _voicePause = true;

                await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback paused.")
                    .MakeSuccess());
            }
        }

        public async Task ResumeAsync()
        {
            if (_textChannel != null)
            {
                if (!IsConnected)
                {
                    await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("I am not connected.")
                        .MakeError());

                    return;
                }

                if (!IsPaused)
                {
                    await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                        .WithAuthor("Playback is not paused.")
                        .MakeError());

                    return;
                }

                await _voiceNextConn.ResumeAsync();

                _voicePause = false;

                await _textChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("Playback resumed.")
                    .MakeSuccess());
            }
        }

        public async Task PlayAsync(RadioStation radioStation)
        {
            if (!IsConnected)
            {
                await _textChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                    .WithAuthor("I am not connected to a channel.")
                    .MakeError());

                return;
            }

            if (IsPlaying)
                await StopAsync();
         
            _radioStation = radioStation;

            await _voiceNextConn.SendSpeakingAsync(true);

            _tokenSource = new CancellationTokenSource();
            _ffmpegThread = new Thread(new ParameterizedThreadStart(x => PlayThread(_tokenSource.Token)));
            _ffmpegThread.Start();

            await _textChannel?.SendMessageAsync(await AddStationInfo(_radioStation, new DiscordEmbedBuilder()
                .WithAuthor("Playback started.")
                .AddField("Station Name", _radioStation.Name)
                .AddField("Station URL", _radioStation.StreamUrl)
                .MakeSuccess()));
        }

        public async Task StopAsync()
        {
            _tokenSource.Cancel();
            _tokenSource.Dispose();
            _tokenSource = null;
            _ffmpegThread = null;

            await _textChannel?.SendMessageAsync(new DiscordEmbedBuilder()
                .WithAuthor("Playback stopped.")
                .MakeSuccess());
        }

        public async void PlayThread(CancellationToken cancellationToken)
        {
            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{_radioStation.StreamUrl}"" -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            _ffmpegProcess.Start();
            _ffmpegStream = _ffmpegProcess.StandardOutput.BaseStream;

            try
            {
                await _ffmpegStream.CopyToAsync(_voiceSink, null, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Log.Info("Playback cancelled, cleaning up!");

                _ffmpegProcess.WaitForExit();

                if (!_ffmpegProcess.HasExited)
                    _ffmpegProcess.Close();

                _ffmpegProcess.Dispose();
                _ffmpegStream.Dispose();
                _ffmpegStream = null;
                _ffmpegProcess = null;

                await _voiceSink.FlushAsync();

                _voiceSink.Dispose();
                _voiceSink = null;
                _radioStation = null;
            }
        }

        public async Task<DiscordEmbedBuilder> AddStationInfo(RadioStation radioStation, DiscordEmbedBuilder embedBuilder)
        {
            Log.Arguments(radioStation);
            Log.Arguments(embedBuilder);

            if (string.IsNullOrEmpty(radioStation.DataUrl) || radioStation.DataUrl == "-")
                return embedBuilder;

            try
            {
                string jsonData = "";

                using (var web = new WebClient())
                    jsonData = await web.DownloadStringTaskAsync(radioStation.DataUrl);

                Log.Verbose(jsonData);

                if (string.IsNullOrEmpty(jsonData))
                    return embedBuilder;

                JObject json = JObject.Parse(jsonData);
                JToken token = json.GetValue("current");

                Log.Arguments(json);
                Log.Arguments(token);

                string imageUrl = token.Value<string>("image");
                string songName = token.Value<string>("song");
                string author = token.Value<string>("interpret");

                if (!string.IsNullOrEmpty(imageUrl))
                    embedBuilder.WithThumbnail(imageUrl);

                if (!string.IsNullOrEmpty(songName) && !string.IsNullOrEmpty(author))
                    embedBuilder.WithTitle($"{author} - {songName}");

                return embedBuilder;
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                return null;
            }
        }

        public void SetText(DiscordChannel text)
            => _textChannel = text;

        public void SetVoice(DiscordChannel voice)
            => _voiceChannel = voice;
    }
}
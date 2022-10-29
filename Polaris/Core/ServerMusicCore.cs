using DSharpPlus.Entities;

using NiveraLib.Logging;

using Polaris.Config;
using Polaris.Core.MusicModules;
using Polaris.Entities;

using System;
using System.Linq;

namespace Polaris.Core
{
    public class ServerMusicCore
    {
        internal LogId logId;

        internal RadioMusicModule radioModule;
        internal LavalinkMusicModule lavalinkModule;

        public bool IsPlaying
        {
            get
            {
                if (lavalinkModule.IsPlaying)
                    return true;

                if (radioModule.IsPlaying)
                    return true;

                return false;
            }
        }

        public bool IsConnected
        {
            get
            {
                if (lavalinkModule.IsServerConnected)
                    return true;

                if (radioModule.IsConnected)
                    return true;

                return false;
            }
        }

        public bool IsLooping
        {
            get => lavalinkModule.IsLooping;
            set => lavalinkModule.IsLooping = value;
        }

        public bool IsPaused
        {
            get
            {
                if (lavalinkModule.IsPaused)
                    return true;

                if (radioModule.IsPaused)
                    return true;

                return false;
            }
        }

        public int Volume
        {
            get
            {
                if (lavalinkModule.IsServerConnected)
                    return lavalinkModule.Volume;

                if (radioModule.IsConnected)
                    return 100;

                return -1;
            }
            set
            {
                if (lavalinkModule.IsServerConnected)
                    lavalinkModule.Volume = value;
            }
        }

        public DiscordChannel AudioChannel
        {
            get
            {
                if (lavalinkModule.IsServerConnected)
                    return lavalinkModule.Voice;

                if (radioModule.IsConnected)
                    return radioModule.Voice;

                return null;
            }
            set
            {
                lavalinkModule.SetVoiceChannel(value);
                radioModule.SetVoice(value);
            }
        }

        public DiscordChannel TextChannel
        {
            get
            {
                if (lavalinkModule.IsServerConnected)
                    return lavalinkModule.Text;

                if (radioModule.IsConnected)
                    return radioModule.Text;

                return null;
            }
            set
            {
                lavalinkModule.SetTextChannel(value);
                radioModule.SetText(value);
            }
        }

        public ServerMusicCore(ulong guildId)
        {
            logId = new LogId("serverMusicCore", (long)guildId);

            radioModule = new RadioMusicModule(this, guildId);
            lavalinkModule = new LavalinkMusicModule(this, guildId);
        }

        public void Join(DiscordChannel voice, DiscordChannel text, string type)
        {
            lavalinkModule.JoinAsync(voice, text);
            radioModule.JoinAsync(voice, text);
        }

        public void Pause()
        {
            if (lavalinkModule.IsPlaying)
                lavalinkModule.PauseAsync();

            if (radioModule.IsPlaying)
                radioModule.PauseAsync();
        }

        public void Resume()
        {
            if (lavalinkModule.IsPaused)
                lavalinkModule.ResumeAsync();

            if (radioModule.IsPaused)
                radioModule.ResumeAsync();
        }

        public void NowPlaying()
        {
            if (lavalinkModule.IsPlaying)
                lavalinkModule.NowPlaying();

            if (radioModule.IsPlaying)
                radioModule.NowPlayingAsync();
        }

        public void Seek(TimeSpan time)
        {
            if (lavalinkModule.IsServerConnected)
                lavalinkModule.SeekAsync(time);
        }

        public void PlayPartial(TimeSpan from, TimeSpan to)
        {
            if (lavalinkModule.IsServerConnected)
                lavalinkModule.PlayPartial(from, to);
        }

        public void Play(UserPlaylist userPlaylist)
        {
            if (lavalinkModule.IsServerConnected)
                lavalinkModule.PlayAsync(userPlaylist);
        }

        public void Skip()
        {
            if (lavalinkModule.IsServerConnected)
                lavalinkModule.SkipAsync();
        }

        public void Play(string query, DiscordUser author = null)
        {
            if (query.StartsWith("radio:"))
            {
                query = query.Replace("radio:", "");

                RadioStation radioStation = GlobalCache.Instance.Stations.FirstOrDefault(x => x.Name.ToLower().Contains(query.ToLower()));

                radioModule.SetText(TextChannel);
                radioModule.SetVoice(AudioChannel);
                radioModule.PlayAsync(radioStation);
            }
            else
            {
                lavalinkModule.PlayAsync(query, AudioChannel, TextChannel, author);
            }
        }

        public void ViewQueue(DiscordUser author)
        {
            if (lavalinkModule.IsServerConnected)
                lavalinkModule.ViewQueueAsync(author);
        }

        public void ClearQueue()
        {
            lavalinkModule.ClearQueueAsync();
        }

        public void Stop()
        {
            if (lavalinkModule.IsPlaying)
                lavalinkModule.StopAsync();

            if (radioModule.IsPlaying)
                radioModule.StopAsync();
        }

        public void Disconnect()
        {
            try
            {
                if (lavalinkModule.IsServerConnected)
                    lavalinkModule.LeaveAsync();

                if (radioModule.IsConnected)
                    radioModule.DisconnectAsync();
            }
            catch { }
        }

        public void SetChannels(DiscordChannel text, DiscordChannel voice)
        {
            AudioChannel = voice;
            TextChannel = text;
        }
    }
}
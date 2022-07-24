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

namespace Polaris.Helpers
{
    public static class UserPlaylistHelper
    {
        public static MusicTrack LavaToPolaris(LavalinkTrack lavalinkTrack)
        {
            return new MusicTrack
            {
                Author = lavalinkTrack.Author,
                Duration = lavalinkTrack.Length,
                Title = lavalinkTrack.Title,
                URL = lavalinkTrack.Uri.AbsoluteUri
            };
        }

        public static List<MusicTrack> LavaListToPolarisList(List<LavalinkTrack> lavalinkTracks)
        {
            List<MusicTrack> tracks = new List<MusicTrack>();

            for (int i = 0; i < lavalinkTracks.Count; i++)
            {
                tracks.Add(new MusicTrack
                {
                    Author = lavalinkTracks[i].Author,
                    Duration = lavalinkTracks[i].Length,
                    Title = lavalinkTracks[i].Title,
                    URL = lavalinkTracks[i].Uri.AbsoluteUri
                });
            }

            return tracks;
        }

        public static UserPlaylist RetrievePlaylist(ulong owner)
        {
            return GlobalConfig.Instance.UserPlaylists.FirstOrDefault(x => x.PlaylistOwner == owner);
        }

        public static UserPlaylist RetrievePlaylist(string id)
        {
            return GlobalConfig.Instance.UserPlaylists.FirstOrDefault(x => x.ID == id);
        }

        public static UserPlaylist CreatePlaylist(string name, string id, string description, ulong owner, bool isPrivate, List<MusicTrack> tracks)
        {
            var playlist = new UserPlaylist
            {
                Description = description,
                ID = id,
                IsPrivate = isPrivate,
                Name = name,
                PlaylistOwner = owner,
                Tracks = tracks
            };

            GlobalConfig.Instance.UserPlaylists.Add(playlist);

            ConfigManager.Save();

            return playlist;
        }
    }
}

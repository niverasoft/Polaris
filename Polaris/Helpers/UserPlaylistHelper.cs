using System.Collections.Generic;
using System.Linq;

using DSharpPlus.Lavalink;

using Polaris.Config;
using Polaris.Entities;

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

        public static UserPlaylist RetrievePlaylist(string idOrName)
        {
            return GlobalConfig.Instance.UserPlaylists.FirstOrDefault(x => x.ID.ToLower() == idOrName.ToLower() || x.Name.ToLower() == idOrName.ToLower());
        }

        public static void DeletePlaylist(UserPlaylist userPlaylist)
        {
            GlobalConfig.Instance.UserPlaylists.Remove(userPlaylist);
            ConfigManager.Save();
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

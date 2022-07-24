using System.Collections.Generic;

namespace Polaris.Entities
{
    public class UserPlaylist
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ID { get; set; }
        public ulong PlaylistOwner { get; set; }
        public bool IsPrivate { get; set; }

        public List<MusicTrack> Tracks { get; set; } = new List<MusicTrack>();
    }
}

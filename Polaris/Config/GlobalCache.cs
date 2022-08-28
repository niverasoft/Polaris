using System.Collections.Generic;

using Polaris.Entities;

namespace Polaris.Config
{
    public class GlobalCache
    {
        public static GlobalCache Instance { get; set; }

        public List<RadioStation> Stations { get; set; } = new List<RadioStation>();

        public Dictionary<ulong, CachedDiscordMember> CachedDiscordMembers { get; set; } = new Dictionary<ulong, CachedDiscordMember>();

        public static CachedDiscordMember GetCachedDiscordMember(ulong id)
        {
            return Instance.CachedDiscordMembers.TryGetValue(id, out var member) ? member : null;
        }
    }
}
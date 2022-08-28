using System;
using System.Collections.Generic;
using System.Text;

namespace Polaris.Config
{
    public class VoiceCache
    {
        public List<byte[]> OpusVoicePackets { get; set; } = new List<byte[]>();
        public List<byte[]> PcmVoicePackets { get; set; } = new List<byte[]>();

        public uint LastSSRC { get; set; }
        public ulong DiscordUserId { get; set; }
    }
}

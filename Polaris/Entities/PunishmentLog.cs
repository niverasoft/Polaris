using Polaris.Enums;
using System;

namespace Polaris.Entities
{
    public class PunishmentLog
    {
        public ulong Member { get; set; }
        public ulong Admin { get; set; }

        public string Reason { get; set; } = "";
        public string Id { get; set; } = "";

        public bool IsPermanent { get; set; } = false;
        public bool HasExpired { get; set; }

        public long RemaningSeconds { get; set; }

        public PunishmentType Type { get; set; } = PunishmentType.ServerWarning;

        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; } = DateTime.Now;
        public DateTime LastUpdateAt { get; set; } = DateTime.Now;

        public ServerRule ServerRule { get; set; } = null;
    }
}

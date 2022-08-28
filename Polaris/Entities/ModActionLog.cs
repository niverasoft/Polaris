using Polaris.Enums;

using System;

namespace Polaris.Entities
{
    public class ModActionLog
    {
        public CachedDiscordMember Member { get; set; }
        public CachedDiscordMember Admin { get; set; }

        public string Reason { get; set; } = "";
        public string Id { get; set; } = "";
        public string PunishmentLogId { get; set; } = "";

        public ModActionType Type { get; set; } = ModActionType.MemberWarning;

        public DateTime IssuedAt { get; set; } = DateTime.Now;
    }
}

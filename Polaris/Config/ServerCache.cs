using Polaris.Entities;

using System;
using System.Collections.Generic;
using System.Text;

namespace Polaris.Config
{
    public class ServerCache
    {
        public Dictionary<ulong, PunishmentLog> PunishmentHistory { get; set; } = new Dictionary<ulong, PunishmentLog>();
    }
}

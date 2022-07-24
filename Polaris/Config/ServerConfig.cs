using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris.Config
{
    public class ServerConfig
    {
        public string Prefix { get; set; } = "!";
        public ulong BotOwner { get; set; } = 0;
        public bool AllowAdminOverride { get; set; } = true;

        public List<string> DisabledCommands { get; set; } = new List<string>();
        public Dictionary<ulong, List<string>> Perms { get; set; } = new Dictionary<ulong, List<string>>();
    }
}

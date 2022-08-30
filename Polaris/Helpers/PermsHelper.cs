using System.Collections.Generic;

namespace Polaris.Helpers
{
    public static class PermsHelper
    {
        public static Dictionary<string, List<string>> Permissions = new Dictionary<string, List<string>>()
        {
            ["Management"] = new List<string>()
            {
                "mgmt.prefix",
                "mgmt.perms",
                "mgmt.giveaways",
                "mgmt.suggestions"
            },

            ["Moderation"] = new List<string>()
            {
                "mod.ban.temp",
                "mod.ban.perm",
                "mod.kick",
                "mod.mute.temp",
                "mod.mute.perm",
                "mod.voicedisconnect",
                "mod.channellock",
                "mod.giveaways.blacklist",
                "mod.suggestions.blacklist"
            },

            ["Utility"] = new List<string>()
            {
                "utils.giveaways"
            },

            ["CustomCommands"] = new List<string>()
            {
                "cc.create",
                "cc.remove",
                "cc.edit",
                "cc.publish"
            },

            ["Music"] = new List<string>()
            {
                "dj"
            }
        };

        public static List<string> ProcessPermissions(string value)
        {
            string[] args = value.Contains(",") ? value.Split(',') : value.Split(' ');

            List<string> validPerms = GetValidPerms();
            List<string> recvPerms = new List<string>();

            foreach (string arg in args)
            {
                if (recvPerms.Contains(arg))
                    continue;

                if (Permissions.TryGetValue(arg, out var list))
                {
                    recvPerms.AddRange(list);
                }
                else
                {
                    if (validPerms.Contains(arg))
                        recvPerms.Add(arg);
                }
            }

            return recvPerms;
        }

        public static List<string> GetValidPerms()
        {
            List<string> perms = new List<string>();

            foreach (var pair in Permissions)
            {
                perms.AddRange(pair.Value);
            }

            return perms;
        }
    }
}

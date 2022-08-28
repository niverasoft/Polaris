using System.Collections.Generic;
using System.Linq;

using Polaris.Config;

using DSharpPlus.Entities;

namespace Polaris.Core
{
    public class ServerPermsCore
    {
        public bool CheckForPerms(DiscordMember author, string permToCheck, CoreCollection coreCollection)
        {
            if (permToCheck == "owner")
            {
                if (author.Id != GlobalConfig.Instance.BotOwnerId)
                    return false;
            }
            else
            {
                if (coreCollection.ServerConfig.AllowAdminOverride && author.Roles.Any(x => x.CheckPermission(DSharpPlus.Permissions.Administrator) == DSharpPlus.PermissionLevel.Allowed))
                    return true;

                if (coreCollection.ServerConfig.Perms.TryGetValue(author.Id, out List<string> perms))
                {
                    if (perms.Contains(permToCheck))
                        return true;
                }

                foreach (var role in author.Roles)
                {
                    if (coreCollection.ServerConfig.Perms.TryGetValue(role.Id, out perms))
                    {
                        if (perms.Contains(permToCheck))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
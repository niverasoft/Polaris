﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DSharpPlus.Entities;

using Polaris.Config;
using Polaris.Discord;

namespace Polaris.Entities
{
    public class CachedDiscordMember
    {
        public ulong Id { get; set; }

        public string Name { get; set; }
        public string Discriminator { get; set; }
        public string AvatarUrl { get; set; }

        public Dictionary<ulong, string> ServerNicknames { get; set; } = new Dictionary<ulong, string>();
        public List<string> KnownUsernames { get; set; } = new List<string>();

        public static CachedDiscordMember Cache(DiscordMember discordMember)
        {
            bool save = false;

            if (GlobalCache.Instance.CachedDiscordMembers.TryGetValue(discordMember.Id, out var cachedDiscordMember))
            {
                if (cachedDiscordMember.Name != discordMember.Username)
                {
                    cachedDiscordMember.KnownUsernames.Add(cachedDiscordMember.Name);
                    cachedDiscordMember.Name = discordMember.Username;

                    save = true;
                }
            }
            else
            {
                cachedDiscordMember = new CachedDiscordMember
                {
                    Discriminator = discordMember.Discriminator,
                    Id = discordMember.Id,
                    Name = discordMember.Username,
                    AvatarUrl = discordMember.GetAvatarUrl(DSharpPlus.ImageFormat.Auto)
                };

                cachedDiscordMember.KnownUsernames.Add(cachedDiscordMember.Name);

                save = true;
            }

            cachedDiscordMember.SaveNicknames(ref save);

            if (save)
            {
                GlobalCache.Instance.CachedDiscordMembers[cachedDiscordMember.Id] = cachedDiscordMember;
                ConfigManager.Save();
            }

            return cachedDiscordMember;
        }

        public async Task<DiscordUser> FindUser()
        {
            return await DiscordNetworkHandlers.GlobalClient.GetUserAsync(Id, true);
        }

        public DiscordMember FindMember()
        {
            var guild = DiscordNetworkHandlers.GlobalClient.Guilds.FirstOrDefault(x => x.Value.Members.ContainsKey(Id)).Value;

            if (guild == null)
                return null;

            return guild.Members[Id];
        }

        public void SaveNicknames(ref bool save)
        {
            foreach (var guild in DiscordNetworkHandlers.GlobalClient.Guilds.Values)
            {
                if (guild.Members.ContainsKey(Id) && !ServerNicknames.ContainsValue(guild.Members[Id].Nickname))
                {
                    if (!ServerNicknames.ContainsKey(guild.Id))
                    {
                        ServerNicknames.Add(guild.Id, guild.Members[Id].Nickname);

                        save = true;
                    }
                    else
                    {
                        if (!ServerNicknames[guild.Id].Contains(guild.Members[Id].Nickname))
                        {
                            ServerNicknames[guild.Id] = guild.Members[Id].Nickname;

                            save = true;
                        }
                    }
                }
            }
        }
    }
}
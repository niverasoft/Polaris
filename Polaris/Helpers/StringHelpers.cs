using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;

namespace Polaris.Helpers
{
    public static class StringHelpers
    {
        private static Regex RoleRegex { get; }
        private static Regex UserRegex { get; }
        private static Regex ChannelRegex { get; }
        private static Regex URLRegex { get; }

        public const string NewLine = "\n";

        static StringHelpers()
        {
            RoleRegex = new Regex(@"^<@&(\d+?)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
            UserRegex = new Regex(@"^<@\!?(\d+?)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
            ChannelRegex = new Regex(@"^<#(\d+?)>$", RegexOptions.ECMAScript | RegexOptions.Compiled);
            URLRegex = new Regex(@"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?");
        }

        public static TimeSpan ParseTimeSpan(string span)
        {
            return TimeSpan.Parse(span);
        }

        public static string[] SplitByNewLine(string str)
        {
            return str.Split(new string[]
            {
                "\r\n",
                "\r",
                "\n"
            }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string FindURL(string str)
        {
            var match = URLRegex.Match(str);

            if (match != null && match.Success)
                return match.Value;

            return null;
        }

        public static DiscordRole FindRole(string value, CommandContext ctx)
        {

            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rid))
            {
                var result = ctx.Guild.GetRole(rid);

                return result;
            }

            var m = RoleRegex.Match(value);

            if (m.Success && ulong.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out rid))
            {
                var result = ctx.Guild.GetRole(rid);

                return result;
            }

            var rol = ctx.Guild.Roles.Values.FirstOrDefault(xr =>
                xr.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase));

            return rol;
        }

        public static DiscordChannel FindChannel(string value, DiscordGuild discordGuild)
        {

            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cid))
            {
                return discordGuild.GetChannel(cid);
            }

            var m = ChannelRegex.Match(value);

            if (m.Success && ulong.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out cid))
            {
                return discordGuild.GetChannel(cid);
            }

            var chan = discordGuild.Channels.Values.FirstOrDefault(xr =>
                xr.Name.Equals(value, StringComparison.InvariantCultureIgnoreCase));

            return chan;
        }

        public static List<DiscordRole> FindRoles(string value, CommandContext ctx)
        {
            List<DiscordRole> roles = new List<DiscordRole>();

            string[] args = value.Contains(",") ? value.Split(',') : value.Split(' ');

            foreach (string arg in args)
            {
                DiscordRole role = FindRole(arg, ctx);

                if (role != null)
                    roles.Add(role);
            }

            return roles;
        }

        public static DiscordMember FindMember(string value, CommandContext ctx)
        {
            if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var uid))
            {
                if (ctx.Guild.Members.TryGetValue(uid, out var member))
                {
                    return member;
                }
            }

            var m = UserRegex.Match(value);

            if (m.Success && ulong.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uid))
            {
                if (ctx.Guild.Members.TryGetValue(uid, out var member))
                {
                    return member;
                }
            }

            var di = value.IndexOf('#');
            var un = di != -1 ? value.Substring(0, di) : value;
            var dv = di != -1 ? value.Substring(di + 1) : null;

            var comparison = StringComparison.InvariantCulture;
            var us = ctx.Guild.Members.Values.Where(xm =>
                (xm.Username.Equals(un, comparison) &&
                 ((dv != null && xm.Discriminator == dv) || dv == null)) || value.Equals(xm.Nickname, comparison));

            var mbr = us.FirstOrDefault();

            return mbr;
        }

        public static List<DiscordMember> FindUsers(string value, CommandContext ctx)
        {
            List<DiscordMember> members = new List<DiscordMember>();

            string[] args = value.Contains(",") ? value.Split(',') : value.Split(' ');

            foreach (string arg in args)
            {
                DiscordMember member = FindMember(arg, ctx);

                if (members != null)
                    members.Add(member);
            }

            return members;
        }

        public static string RemoveBeforeIndex(string str, int index)
        {
            string st = "";

            for (int i = index + 1; i < str.Length; i++)
            {
                st += str[i];
            }

            return st;
        }

        public static string RemoveAfterIndex(string str, int index)
        {
            string st = "";

            for (int i = 0; i < index; i++)
            {
                st += str[i];
            }

            return st;
        }
    }
}

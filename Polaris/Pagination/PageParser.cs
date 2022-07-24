using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Executors;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.Net.Models;
using DSharpPlus.Net.Serialization;
using DSharpPlus.Net.Udp;
using DSharpPlus.Net.WebSocket;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.VoiceNext;
using DSharpPlus.VoiceNext.Codec;
using DSharpPlus.VoiceNext.EventArgs;

using Polaris.Config;
using Polaris.Boot;
using Polaris.Core;
using Polaris.Discord;
using Polaris.Entities;
using Polaris.Enums;
using Polaris.Helpers;
using Polaris.Properties;

namespace Polaris.Pagination
{
    public static class PageParser
    {
        private static List<IPageParserBase> parsers = new List<IPageParserBase>();

        static PageParser()
        {
            AddParser(new LavalinkTrackParser());
        }

        public static void AddParser(IPageParserBase parser)
        {
            parsers.Add(parser);
        }

        public static List<Page> SplitToPages<T>(List<T> items)
        {
            List<Page> pages = new List<Page>();

            var parser = parsers.FirstOrDefault(x => x.TargetType == typeof(T));

            if (parser == null)
                return null;

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();

            foreach (var item in items)
            {
                (parser as IPageParser<T>).Parse(item, builder, pages);
            }

            return pages;
        }

        public static double GetTotalAmount(List<Page> pages)
        {
            double amount = 0;

            foreach (Page page in pages)
            {
                string[] lines = StringHelpers.SplitByNewLine(page.Embed.Description);

                amount += lines.Length;
            }

            return amount;
        }

        public static List<Page> SplitToPages<T>(List<T> items, DiscordEmbedBuilder builder)
        {
            List<Page> pages = new List<Page>();

            var parser = parsers.FirstOrDefault(x => x.TargetType == typeof(T));

            if (parser == null)
                return null;

            foreach (var item in items)
            {
                (parser as IPageParser<T>).Parse(item, builder, pages);
            }

            return pages;
        }
    }

    public interface IPageParserBase
    {
        public Type TargetType { get; }
    }

    public interface IPageParser<T> : IPageParserBase
    {
        public void Parse(T item, DiscordEmbedBuilder discordEmbedBuilder, List<Page> pages);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

using Polaris.Helpers;

namespace Polaris.Pagination
{
    public static class PageParser
    {
        private static List<IPageParserBase> parsers = new List<IPageParserBase>();

        static PageParser()
        {
            AddParser(new LavalinkTrackParser());
            AddParser(new MusicTrackParser());
            AddParser(new RadioStationParser());
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
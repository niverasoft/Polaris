using System;
using System.Linq;
using System.Collections.Generic;

using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;

using Polaris.Helpers;

namespace Polaris.Pagination
{
    public class LavalinkTrackParser : IPageParser<LavalinkTrack>
    {
        public Type TargetType => typeof(LavalinkTrack);

        public void Parse(LavalinkTrack item, DiscordEmbedBuilder discordEmbedBuilder, List<Page> pages)
        {
            if (pages.Count < 1)
            {
                pages.Add(new Page
                {
                    Embed = discordEmbedBuilder.WithDescription($"{NumberEmotes.One} [{item.Title}]({item.Uri.AbsoluteUri}) [{item.Length.ToString()}]\n")
                });
            }
            else
            {
                var page = pages.Last();
                var embed = page.Embed;
                var builder = new DiscordEmbedBuilder(embed);

                string[] lines = StringHelpers.SplitByNewLine(builder.Description);

                if (lines.Length + 1 < EmbedLimits.ItemsPerPage)
                {
                    builder.Description += $"{Environment.NewLine}{NumberEmotes.NumberToEmote(PageParser.GetTotalAmount(pages) + 1)} [{item.Title}]({item.Uri.AbsoluteUri}) [{item.Length.ToString()}]\n";

                    page.Embed = builder.Build();
                }
                else
                {
                    pages.Add(new Page
                    {
                        Embed = new DiscordEmbedBuilder(discordEmbedBuilder.Build()).WithDescription($"{NumberEmotes.NumberToEmote(PageParser.GetTotalAmount(pages) + 1)} [{item.Title}]({item.Uri.AbsoluteUri}) [{item.Length.ToString()}]\n")
                    });
                }
            }
        }
    }
}

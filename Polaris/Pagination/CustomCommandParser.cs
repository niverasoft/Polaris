using DSharpPlus.Entities;
using DSharpPlus.Interactivity;

using Polaris.CustomCommands;

using System;
using System.Collections.Generic;

namespace Polaris.Pagination
{
    public class CustomCommandParser : IPageParser<CustomCommand>
    {
        public Type TargetType => typeof(CustomCommand);

        public void Parse(CustomCommand item, DiscordEmbedBuilder discordEmbedBuilder, List<Page> pages)
        {
            pages.Add(new Page
            {
                Embed = new DiscordEmbedBuilder()
                .WithTitle(item.Name)
                .AddField("Description", item.Description, true)
                .AddField("Author", item.Author, true)
            });
        }
    }
}

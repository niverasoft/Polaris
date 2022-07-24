using System;
using System.IO;
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

using Nivera;

namespace Polaris.Core
{
    public class ServerCore
    {
        public int CoreId { get; set; }

        public DiscordClient Client;
        public DiscordGuild Guild;

        public CoreCollection CoreCollection;

        public bool LastAnnouncementSent;

        public async Task LoadAsync(ServerConfig serverConfig, DiscordClient client, DiscordGuild guild)
        {
            CoreCollection = new CoreCollection(this, serverConfig, guild.Id);
            Client = client;
            Guild = guild;
            CoreId = GetNextServerCoreId();

            Log.JoinCategory($"cores/server");

            ConfigManager.ConfigList[Guild.Id] = CoreCollection.ServerConfig;
            ConfigManager.Save();

            InstallEventHandlers();
        }

        public async Task LoadAsync(DiscordClient client, DiscordGuild guild)
        {
            CoreCollection = new CoreCollection(this, new ServerConfig(), guild.Id);
            CoreCollection.ServerConfig.BotOwner = guild.Owner != null ? guild.Owner.Id : guild.Members.Where(x => x.Value.Roles.Any(z => z.CheckPermission(Permissions.Administrator) == PermissionLevel.Allowed)).First().Key;
            Client = client;
            Guild = guild;
            CoreId = GetNextServerCoreId();

            Log.JoinCategory($"cores/server");

            ConfigManager.ConfigList[Guild.Id] = CoreCollection.ServerConfig;
            ConfigManager.Save();

            InstallEventHandlers();
        }

        public void InstallEventHandlers()
        {
            Client.MessageCreated += (x, e) =>
            {
                if (e.Guild.Id != Guild.Id)
                    return Task.CompletedTask;

                var cnext = DiscordNetworkHandlers.GlobalClient.GetCommandsNext();
                var msg = e.Message;

                var cmdStart = msg.GetStringPrefixLength(CoreCollection.ServerConfig.Prefix.ToString());

                if (cmdStart == -1)
                    return Task.CompletedTask;

                var prefix = msg.Content.Substring(0, cmdStart);
                var cmdString = msg.Content.Substring(cmdStart);

                var command = cnext.FindCommand(cmdString, out var args);

                if (command == null)
                    return Task.CompletedTask;

                var ctx = cnext.CreateContext(msg, prefix, command, args);

                Task.Run(async () => await cnext.ExecuteCommandAsync(ctx));

                return Task.CompletedTask;
            };
        }

        public static int GetNextServerCoreId()
            => DiscordNetworkHandlers.ServerCores.Count + 1;
    }
}
using DSharpPlus.EventArgs;
using DSharpPlus.CommandsNext;

using Nivera;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polaris.Core;
using Polaris.Discord;
using Polaris.Config;

namespace Polaris.CustomCommands
{
    public static class CustomCommandManager
    {
        private static List<CustomCommand> _loadedCommands = new List<CustomCommand>();
        private static List<CustomCommand> _erroredCommands = new List<CustomCommand>();

        public static bool IsEnabled;

        static CustomCommandManager()
        {
            Log.JoinCategory("customcommands");

            IsEnabled = GlobalConfig.Instance.AllowCustomCommands;

            SaveCommand(null);
        }

        public static List<CustomCommand> GetCommandsForGuild(ulong guildId, bool includeGlobal)
            => _loadedCommands.Where(x => x.Guild == guildId && (includeGlobal ? x.IsGlobal : true)).ToList();

        public static bool TryGetCommand(CoreCollection cores, MessageCreateEventArgs e, out CustomCommand customCommand, out string prefixWithCommand)
        {
            customCommand = null;
            prefixWithCommand = "";

            try
            {
                if (!_loadedCommands.Any(x => x.Guild == e.Guild.Id))
                    return false;
                var msg = e.Message;
                var cmdStart = msg.GetStringPrefixLength(cores.ServerConfig.Prefix.ToString());
                if (cmdStart == -1)
                    return false;
                var prefix = msg.Content.Substring(0, cmdStart);
                var cmdString = msg.Content.Substring(cmdStart);
                var cmd = cmdString.Split(' ')[0];
                prefixWithCommand = $"{prefix}{cmdString}";
                customCommand = _loadedCommands.FirstOrDefault(x => x.Name.ToLower() == cmd.ToLower());
                return customCommand != null;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
        }

        public static async Task<bool> TryInvokeCommand(string prefixWithCommandStr, MessageCreateEventArgs e, CustomCommand customCommand)
        {
            return await customCommand.ExecuteAsync(
                     DiscordNetworkHandlers.GlobalClient,
                     e.Guild.Members[e.Author.Id],
                     e.Message,
                     e.Message.Content.Replace(prefixWithCommandStr, "").Split(' '));
        }

        public static string SerializeCommand(CustomCommand customCommand)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"[{customCommand.Name}] [LINEBREAK]");
            stringBuilder.AppendLine($"[{customCommand.Description}] [LINEBREAK]");
            stringBuilder.AppendLine($"[{customCommand.Author}] [LINEBREAK]");
            stringBuilder.AppendLine($"[{customCommand.Guild}] [LINEBREAK]");
            stringBuilder.AppendLine($"[{(customCommand.IsGlobal ? "GlobalCommand" : "ServerCommand")} [LINEBREAK]");
            stringBuilder.AppendLine("[SOURCE START] [LINEBREAK]");
            foreach (var line in customCommand.Source)
                stringBuilder.AppendLine($"{line} [LINEBREAK]");
            stringBuilder.AppendLine("[SOURCE END] [LINEBREAK]");
            Log.Verbose("Command serialized.");
            Log.Verbose(stringBuilder);
            return stringBuilder.ToString();
        }

        public static CustomCommand DeserializeCommand(string commandInfo)
        {
            string[] lines = commandInfo.Split("[LINEBREAK]");

            CustomCommand customCommand = new CustomCommand
            {
                Name = lines[0].Remove(0, 1).Remove(lines[0].Length - 1, 1),
                Description = lines[1].Remove(0, 1).Remove(lines[0].Length - 1, 1),
                Author = lines[2].Remove(0, 1).Remove(lines[0].Length - 1, 1),
                Guild = ulong.Parse(lines[3].Remove(0, 1).Remove(lines[0].Length - 1, 1)),
                IsGlobal = lines[4].Contains("GlobalCommand")
            };

            List<string> source = new List<string>();
            string curLine = "";
            int index = 6;

            while (curLine != "[SOURCE END] [LINEBREAK]")
            {
                curLine = lines[index];
                source.Add(curLine);
                index++;
            }

            customCommand.Source = source.ToArray();
            source.Clear();
            source = null;

            return customCommand;
        }

        public static void LoadCommand(string file)
        {
            string fileName = Path.GetFileName(file);
            if (!fileName.StartsWith("CustomCommand_"))
                return;
            if (!Path.GetExtension(file).Contains("txt"))
                return;
            Log.Info($"Loading Custom Command: {fileName.Replace("CustomCommand_", "")}");
            var command = DeserializeCommand(File.ReadAllText(file));
            Log.Info("Command data read, compiling command ..");
            var result = new CustomCommandCompiler().CompileCustomCommand(command);
            if (result.Status == CompilerStatus.CompilerSuccess)
            {
                Log.Info("Command compiled.");
                _loadedCommands.Add(result.Command);
            }
            else
            {
                Log.Error("Failed to compile custom command!");
                Log.Arguments(result);
                _erroredCommands.Add(result.Command);
            }
        }

        public static void LoadCommands()
        {
            Log.Info($"Loading Custom Commands ..");
            foreach (var directory in Directory.GetDirectories($"{Directory.GetCurrentDirectory()}/CustomCommands"))
            {
                Log.Info($"Loading Custom Commands for {Path.GetDirectoryName(directory)} ..");
                foreach (var file in Directory.GetFiles(directory))
                {
                    LoadCommand(file);
                }
            }
            Log.Info($"Loaded Custom Commands!");
        }

        public static void SaveCommands()
        {
            foreach (var cc in _loadedCommands)
            {
                SaveCommand(cc);
            }
        }

        public static void AddCommand(CustomCommand customCommand)
        {
            _loadedCommands.Add(customCommand);
            SaveCommand(customCommand);
        }

        public static void SaveCommand(CustomCommand customCommand)
        {
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/CustomCommands"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/CustomCommands");
            if (customCommand == null)
                return;
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/CustomCommands/{customCommand.Guild}"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/CustomCommands/{customCommand.Guild}");
            File.WriteAllText($"{Directory.GetCurrentDirectory()}/CustomCommands/{customCommand.Guild}/CustomCommand_{customCommand.Name}.txt", SerializeCommand(customCommand));
            Log.Verbose($"CustomCommands@{customCommand.Guild}: Saved command {customCommand.Name}");
        }
    }
}

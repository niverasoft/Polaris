using DSharpPlus.Entities;

using Nivera;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Polaris.Discord;
using Polaris.Config;
using Newtonsoft.Json;

namespace Polaris.CustomCommands
{
    public static class CustomCommandManager
    {
        private static List<CustomCommand> _loadedCommands = new List<CustomCommand>();
        private static List<CustomCommand> _erroredCommands = new List<CustomCommand>();

        public static bool IsEnabled { get; }
        public static CustomCommandCompiler Compiler { get; }

        static CustomCommandManager()
        {
            Log.JoinCategory("customcommands");

            IsEnabled = GlobalConfig.Instance.AllowCustomCommands;
            Compiler = new CustomCommandCompiler();

            SaveCommand(null);
        }

        public static List<CustomCommand> GetCommandsForGuild(ulong guildId, bool includeGlobal)
            => _loadedCommands.Where(x => x.Guild == guildId && (includeGlobal ? x.IsGlobal : true))?.ToList() ?? new List<CustomCommand>();

        public static bool TryGetCommand(string commandName, out CustomCommand command)
        {
            command = _loadedCommands.FirstOrDefault(x => x.Name.ToLower() == commandName.ToLower());

            return command != null;
        }

        public static async Task<bool> TryInvokeCommand(string args, DiscordMember author, DiscordMessage message, CustomCommand customCommand)
        {
            return await customCommand.ExecuteAsync(
                     DiscordNetworkHandlers.GlobalClient,
                     author,
                     message,
                     args.Split(' '));
        }

        public static string SerializeCommand(CustomCommand customCommand)
        {
            return JsonConvert.SerializeObject(customCommand, Formatting.Indented);
        }

        public static CustomCommand DeserializeCommand(string commandInfo)
        {
            return JsonConvert.DeserializeObject<CustomCommand>(commandInfo);
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

            var result = Compiler.CompileCustomCommand(command);

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
                    try
                    {
                        LoadCommand(file);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex); 
                    }
                }
            }

            Log.Info($"Loaded Custom Commands!");

            if (_erroredCommands.Count > 0)
                Log.Warn($"Failed to load {_erroredCommands.Count} command(s).");
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

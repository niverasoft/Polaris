using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Nivera;
using Polaris.CustomCommands.CommandInstructions;
using Polaris.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Polaris.CustomCommands
{
    public class CustomCommand
    {
        private List<ICommandInstruction> _commandInstructions = new List<ICommandInstruction>();

        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string[] Source { get; set; }

        public ulong Guild { get; set; }

        public bool IsGlobal { get; set; }

        public void EmitInstruction(ICommandInstruction commandInstruction)
            => _commandInstructions.Add(commandInstruction);

        public void ClearInstructions()
            => _commandInstructions.Clear();

        public async Task<bool> ExecuteAsync(DiscordClient client, DiscordMember author, DiscordMessage message, string[] rawArguments)
        {
            Log.Verbose($"Executing custom command: {Name}");
            Log.Verbose(Source);

            CommandMemory commandMemory = new CommandMemory();

            Log.Verbose($"Command memory allocated.");

            CommandExecutionContext commandExecutionContext = new CommandExecutionContext
            {
                Author = author,
                Client = client,
                Guild = message.Channel.Guild,
                Message = message,
                RawArguments = rawArguments,
                RawArgumentsString = string.Join(" ", rawArguments),
                Command = this
            };

            Log.Verbose($"Command context created.");
            Log.Verbose($"Executing command instructions.");

            foreach (var instruction in _commandInstructions)
            {
                Log.Verbose($"Executing instruction: {instruction.InstructionAsString()}");

                try
                {
                    var result = await instruction.ExecuteAsync(commandExecutionContext, commandMemory);

                    if (!result.IsSuccess)
                    {
                        Log.Error("Command execution failed!");
                        Log.Error(result.ErrorId);
                        Log.Error(result.ErrorReason);

                        await message.Channel.SendMessageAsync(new DiscordEmbedBuilder()
                            .WithAuthor("Failed to execute this custom command!")
                            .WithTitle(result.ErrorReason)
                            .WithFooter(result.ErrorId)
                            .MakeError());

                        return false;
                    }

                    Log.Verbose($"Instruction succesfully executed.");

                    if (result.ValueReturned)
                    {
                        commandMemory.WriteMemory(result.VariableName, result.ReturnedObject);

                        Log.Verbose($"Instruction returned an object ({result.ReturnedObject}), written to memory.");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }

            Log.Verbose("Command execution finished succesfully.");

            commandMemory.ClearMemory();
            commandMemory = null;
            commandExecutionContext = null;

            return true;
        }
    }
}
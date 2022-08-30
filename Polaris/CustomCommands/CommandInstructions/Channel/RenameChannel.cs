using Polaris.Helpers;

using System.Linq;
using System.Threading.Tasks;

using DSharpPlus.Entities;

namespace Polaris.CustomCommands.CommandInstructions.Channel
{
    public class RenameChannel : ICommandInstruction
    {
        private string _compiledArgs;

        public RenameChannel() { }
        public RenameChannel(string args) { _compiledArgs = args; }

        public InstructionType Type { get; } = InstructionType.RenameChannel;

        public int RequiredArguments { get; } = 2;
        public int OptionalArguments { get; } = 0;

        public bool CheckArguments(string args, CompilerResult compilerResult)
        {
            return true;
        }

        public bool CheckParsingLine(string line, CompilerResult compilerResult)
        {
            if (line.Replace(Type.ToString(), "").Split(' ').Length < RequiredArguments)
            {
                compilerResult.ParseLine = line;
                compilerResult.ErrorId = "CI010";
                compilerResult.ErrorReason = "You need to specify function arguments, either variable, solid or memory arguments.";
                compilerResult.Status = CompilerStatus.CompilerError;

                return false;
            }
                
            return true;
        }

        public ICommandInstruction CreateCopyWithArgs(string args)
        {
            return new RenameChannel(args);
        }

        public async Task<CommandInstructionResult> ExecuteAsync(CommandExecutionContext commandExecutionContext, CommandMemory commandMemory)
        {
            string[] args = _compiledArgs.Split(' ');
            string[] sArgs = args.Skip(1).ToArray();

            var channel = _compiledArgs.StartsWith("$@") ? commandMemory.ReadMemory<DiscordChannel>(args[0].Replace("$@", "")) : StringHelpers.FindChannel(ArgumentHelper.ReplaceVariable(commandExecutionContext.RawArguments, _compiledArgs.Replace(args[0], "")), commandExecutionContext.Guild);

            if (channel == null)
                return new CommandInstructionResult
                {
                    ErrorId = "CI001",
                    ErrorReason = "Invalid command argument at position 1.",
                    IsSuccess = false,
                    ReturnedObject = null,
                    ValueReturned = false,
                    VariableName = ""
                };

            await channel.ModifyAsync(x =>
            {
                x.Name = ArgumentHelper.ReplaceVariable(commandExecutionContext.RawArguments, string.Join(" ", _compiledArgs.Skip(1)));
            });

            return new CommandInstructionResult
            {
                ErrorId = null,
                ErrorReason = null,
                IsSuccess = true,
                ReturnedObject = null,
                ValueReturned = false,
                VariableName = ""
            };
        }

        public string InstructionAsString()
        {
            if (!string.IsNullOrEmpty(_compiledArgs))
                return $"RenameChannel {_compiledArgs}";
            else
                return "RenameChannel $1 $_";
        }
    }
}

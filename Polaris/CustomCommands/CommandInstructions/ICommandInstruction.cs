using System.Threading.Tasks;

namespace Polaris.CustomCommands.CommandInstructions
{
    public interface ICommandInstruction
    {
        InstructionType Type { get; }
        
        int RequiredArguments { get; }
        int OptionalArguments { get; }

        string InstructionAsString();

        bool CheckArguments(string args, CompilerResult compilerResult);
        bool CheckParsingLine(string line, CompilerResult compilerResult);

        Task<CommandInstructionResult> ExecuteAsync(CommandExecutionContext commandExecutionContext, CommandMemory commandMemory);

        ICommandInstruction CreateCopyWithArgs(string args);
    }
}
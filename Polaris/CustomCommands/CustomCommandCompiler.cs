using Polaris.CustomCommands.CommandInstructions;

using System;

namespace Polaris.CustomCommands
{
    public class CustomCommandCompiler
    {
        private Type _instructions;

        public CustomCommandCompiler()
        {
            _instructions = typeof(Instructions);
        }

        public CompilerResult CompileCustomCommand(CustomCommand command)
        {
            string[] lines = command.Source;

            if (lines == null || lines.Length < 0)
            {
                return new CompilerResult
                {
                    ErrorId = "CCC01",
                    ErrorIndex = 0,
                    ErrorReason = "There are no command instructions.",
                    Status = CompilerStatus.CompilerError,
                    TotalLines = 0,
                    ParseLine = "",
                    Lines = lines
                };
            }

            var result = new CompilerResult
            {
                TotalLines = lines.Length,
                Lines = lines,
                ParseLine = "",
                Command = command
            };

            for (int i = 0; i < lines.Length; i++) 
            {
                var line = lines[i];

                if (line.StartsWith("#"))
                    continue;

                string[] lineArgs = line.Split(' ');

                if (lineArgs.Length < 1)
                    continue;

                string instructionName = lineArgs[0];

                var field = _instructions.GetField(instructionName);

                if (field == null)
                {
                    result.ErrorId = "CCC02";
                    result.ErrorIndex = i;
                    result.ErrorReason = $"CCC02: Invalid instruction on line {i} ({instructionName}).";
                    result.ParseLine = line;
                    result.Status = CompilerStatus.CompilerError;

                    return result;
                }

                var instruction = field.GetValue(null) as ICommandInstruction;

                if (!instruction.CheckParsingLine(line, result))
                    return result;

                string clearLine = line.Replace(instructionName, "");

                if (!string.IsNullOrEmpty(clearLine))
                {
                    if (!instruction.CheckArguments(clearLine, result))
                        return result;

                    command.EmitInstruction(instruction.CreateCopyWithArgs(clearLine));
                }
                else
                    command.EmitInstruction(instruction);                
            }

            result.Status = CompilerStatus.CompilerSuccess;

            return result;
        }
    }
}

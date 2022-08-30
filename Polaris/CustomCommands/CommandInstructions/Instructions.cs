using Polaris.CustomCommands.CommandInstructions.Channel;

namespace Polaris.CustomCommands.CommandInstructions
{
    public static class Instructions
    {
        public static RenameChannel RenameChannel = new RenameChannel($"$1 $_");
    }
}
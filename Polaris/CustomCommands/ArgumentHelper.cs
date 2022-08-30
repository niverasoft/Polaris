namespace Polaris.CustomCommands
{
    public static class ArgumentHelper
    {
        public static string ReplaceVariable(string[] args, string variable)
        {
            if (variable == "$_")
                return string.Join(" ", args);

            int index = int.Parse(variable.Replace("$", ""));

            if (index > args.Length)
                return "";

            return args[index];
        }
    }
}

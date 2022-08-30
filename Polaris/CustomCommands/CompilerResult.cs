namespace Polaris.CustomCommands
{
    public class CompilerResult
    {
        public CompilerStatus Status { get; set; }

        public CustomCommand Command { get; set; }

        public string ErrorReason { get; set; }
        public string ErrorId { get; set; }
        public string ParseLine { get; set; }
        public string[] Lines { get; set; }

        public int ErrorIndex { get; set; }
        public int TotalLines { get; set; }
    }
}
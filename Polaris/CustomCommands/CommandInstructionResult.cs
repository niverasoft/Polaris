namespace Polaris.CustomCommands
{
    public class CommandInstructionResult
    {
        public object ReturnedObject { get; set; }
        public string VariableName { get; set; }

        public bool IsSuccess { get; set; }
        public bool ValueReturned { get; set; }

        public string ErrorId { get; set; }
        public string ErrorReason { get; set; }
    }
}
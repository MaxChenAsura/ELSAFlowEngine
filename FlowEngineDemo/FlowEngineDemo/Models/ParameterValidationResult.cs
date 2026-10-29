namespace FlowEngineDemo.Models
{
    public class ParameterValidationResult
    {
        public bool IsValid { get; set; }

        public List<string> Errors { get; set; } = new();

        public Dictionary<string, string?> Parameters { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }
}
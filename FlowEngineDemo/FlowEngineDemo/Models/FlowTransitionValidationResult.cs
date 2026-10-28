namespace FlowEngineDemo.Models
{
    public class FlowTransitionValidationResult
    {
        public bool IsValid { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public string? NextStepCode { get; set; }

        public FlowStepModel? NextStep { get; set; }
    }
}
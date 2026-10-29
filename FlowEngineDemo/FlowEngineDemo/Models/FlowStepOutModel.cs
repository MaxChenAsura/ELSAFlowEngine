namespace FlowEngineDemo.Models
{
    public class FlowStepOutModel
    {
        public string FlowCode { get; set; } = string.Empty;

        public int VersionNo { get; set; }

        public string StepCode { get; set; } = string.Empty;

        public string OutCode { get; set; } = string.Empty;

        public string? OutName { get; set; }

        public string ResultCode { get; set; } = string.Empty;

        public string RouteType { get; set; } = string.Empty;

        public int Priority { get; set; }

        public bool IsDefault { get; set; }

        public string? Remark { get; set; }
    }
}
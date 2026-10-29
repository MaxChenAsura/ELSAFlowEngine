namespace FlowEngineDemo.Models
{
    public class FlowOutConditionModel
    {
        public long Id { get; set; }

        public string FlowCode { get; set; } = string.Empty;

        public int VersionNo { get; set; }

        public string StepCode { get; set; } = string.Empty;

        public string OutCode { get; set; } = string.Empty;

        // 新增這個屬性
        public int ConditionNo { get; set; }

        public string ParamName { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public string? CompareValue { get; set; }
    }
}
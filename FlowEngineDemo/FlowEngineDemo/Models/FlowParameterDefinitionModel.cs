namespace FlowEngineDemo.Models
{
    public class FlowParameterDefinitionModel
    {
        public string FlowCode { get; set; } = string.Empty;

        public int VersionNo { get; set; }

        public string ParamName { get; set; } = string.Empty;

        public string? ParamDesc { get; set; }

        public string DataType { get; set; } = string.Empty;

        public bool IsRequired { get; set; }

        public string? DefaultValue { get; set; }

        public bool IsMutable { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
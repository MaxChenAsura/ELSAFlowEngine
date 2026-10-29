namespace FlowEngineDemo.Models
{
    public class RoutingRequest
    {
        // 流程實例 ID
        public string FlowInstanceId { get; set; } = string.Empty;

        // 目前步驟
        public string CurrentStepCode { get; set; } = string.Empty;

        // 簽核結果，例如 APPROVE / REJECT
        public string ResultCode { get; set; } = string.Empty;

        // 外部指定 OUT_CODE
        public string? ExternalOutCode { get; set; }

        // 外部指定下一步
        public string? ExternalNextStepCode { get; set; }
    }
}
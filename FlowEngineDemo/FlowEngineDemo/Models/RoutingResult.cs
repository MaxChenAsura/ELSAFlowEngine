namespace FlowEngineDemo.Models
{
    public class RoutingResult
    {
        // 是否成功找到路由
        public bool Success { get; set; }

        // 流程實例 ID
        public string FlowInstanceId { get; set; } = string.Empty;

        // 流程代碼
        public string FlowCode { get; set; } = string.Empty;

        // 流程版本
        public int VersionNo { get; set; }

        // 路由模式
        public string RoutingMode { get; set; } = string.Empty;

        // 目前步驟
        public string CurrentStepCode { get; set; } = string.Empty;

        // 簽核結果
        public string ResultCode { get; set; } = string.Empty;

        // 最後選擇的 OUT
        public string? SelectedOutCode { get; set; }

        // 下一個步驟
        public string? NextStepCode { get; set; }

        // 是否為流程結束
        public bool IsCompleted { get; set; }

        // 訊息
        public string Message { get; set; } = string.Empty;
    }
}
namespace FlowEngineDemo.Models
{
    /// <summary>
    /// 簽核人員解析時需要的上下文資料
    /// </summary>
    public class FlowApproverResolveContext
    {
        /// <summary>
        /// 流程代碼，例如 PURCHASE
        /// </summary>
        public string FlowCode { get; set; } = string.Empty;

        /// <summary>
        /// 流程版本
        /// </summary>
        public int VersionNo { get; set; }

        /// <summary>
        /// 關卡代碼，例如 MGR01
        /// </summary>
        public string StepCode { get; set; } = string.Empty;

        /// <summary>
        /// 申請人員工編號
        /// </summary>
        public string ApplicantId { get; set; } = string.Empty;

        /// <summary>
        /// 表單類型
        /// </summary>
        public string FormType { get; set; } = string.Empty;

        /// <summary>
        /// 表單編號
        /// </summary>
        public string FormId { get; set; } = string.Empty;
    }
}
namespace FlowEngineDemo.Models;

public class SubmitApprovalRequest
{
    /// <summary>
    /// 表單種類，例如 PURCHASE、LEAVE
    /// </summary>
    public string FormType { get; set; } = string.Empty;

    /// <summary>
    /// 外部系統的表單編號
    /// </summary>
    public string FormId { get; set; } = string.Empty;

    /// <summary>
    /// 申請人工號
    /// </summary>
    public string ApplicantId { get; set; } = string.Empty;

    /// <summary>
    /// 流程代碼，例如 GENERAL_APPROVAL
    /// </summary>
    public string FlowCode { get; set; } = string.Empty;

    /// <summary>
    /// 表單標題
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// 簽核模式：SINGLE / COUNTERSIGN / PARALLEL
    /// </summary>
    public string ApprovalMode { get; set; } = "SINGLE";

    /// <summary>
    /// 簽核人員，多人時以逗號分隔
    /// </summary>
    public string ApproverIds { get; set; } = string.Empty;
}
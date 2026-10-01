namespace FlowEngineDemo.Models;

public class ApproveApprovalRequest
{
    /// <summary>
    /// Elsa Workflow Instance ID
    /// </summary>
    public string FlowInstanceId { get; set; } = string.Empty;
   
    /// <summary>
    /// 簽核人工號
    /// </summary>
    public string ApproverId { get; set; } = string.Empty;

    /// <summary>
    /// 簽核意見
    /// </summary>
    public string? Comment { get; set; }
}
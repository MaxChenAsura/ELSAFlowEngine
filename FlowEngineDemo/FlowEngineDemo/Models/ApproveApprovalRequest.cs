namespace FlowEngineDemo.Models;

public class ApproveApprovalRequest
{
      // Flow Engine Task ID
    public long TaskId { get; set; }

    // 簽核人員
    public string ApproverId { get; set; } = string.Empty;

    // 簽核意見
    public string? Comment { get; set; }
}
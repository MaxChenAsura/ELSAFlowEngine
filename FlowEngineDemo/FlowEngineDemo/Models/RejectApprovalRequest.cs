namespace FlowEngineDemo.Models;

public class RejectApprovalRequest
{
   // Flow Engine Task ID
    public long TaskId { get; set; }

    // 簽核人員
    public string ApproverId { get; set; } = string.Empty;

    // 退回原因
    public string? Comment { get; set; }
}
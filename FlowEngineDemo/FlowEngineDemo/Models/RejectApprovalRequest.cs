namespace FlowEngineDemo.Models;

public class RejectApprovalRequest
{
    public string FlowInstanceId { get; set; } = string.Empty;

    public string ApproverId { get; set; } = string.Empty;

    public string? Comment { get; set; }
}
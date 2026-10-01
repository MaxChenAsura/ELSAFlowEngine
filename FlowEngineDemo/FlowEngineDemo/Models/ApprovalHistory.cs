namespace FlowEngineDemo.Models;

public class ApprovalHistory
{
    public long Id { get; set; }

    public string FlowInstanceId { get; set; } = string.Empty;

    public string? FormType { get; set; }

    public string? FormId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ApproverId { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public DateTimeOffset ActionAt { get; set; }
}
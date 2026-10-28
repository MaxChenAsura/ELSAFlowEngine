public class ApprovalTask
{
    public long Id { get; set; }

    public string FlowInstanceId { get; set; } = string.Empty;
    // Elsa Bookmark ID（關卡執行時的等待點）
    public string? BookmarkId { get; set; }

    public string FormType { get; set; } = string.Empty;

    public string FormId { get; set; } = string.Empty;

    public string StepCode { get; set; } = string.Empty;

    public string? StepName { get; set; }

    public string ApprovalMode { get; set; } = string.Empty;

    public string ApproverId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
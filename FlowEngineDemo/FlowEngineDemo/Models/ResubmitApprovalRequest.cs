namespace FlowEngineDemo.Models.Requests;

public class ResubmitApprovalRequest
{
    public long TaskId { get; set; }

    public string ApplicantId { get; set; } = string.Empty;

    public string? Comment { get; set; }
}
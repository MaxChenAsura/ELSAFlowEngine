namespace FlowEngineDemo.Models;

public class SubmitApprovalResponse
{
    public string FlowInstanceId { get; set; } = string.Empty;

    public string FormType { get; set; } = string.Empty;

    public string FormId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
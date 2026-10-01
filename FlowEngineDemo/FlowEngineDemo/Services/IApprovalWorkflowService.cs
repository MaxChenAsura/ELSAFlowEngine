namespace FlowEngineDemo.Services;

public interface IApprovalWorkflowService
{
    Task ApproveAsync(
        string flowInstanceId,
        string approverId,
        string? comment);
    Task RejectAsync(
        string flowInstanceId,
        string approverId,
        string? comment);
}
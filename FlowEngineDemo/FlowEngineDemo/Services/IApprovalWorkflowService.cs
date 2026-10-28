namespace FlowEngineDemo.Services;

public interface IApprovalWorkflowService
{
   Task ApproveAsync(
        long taskId,
        string approverId,
        string? comment);

    Task RejectAsync(
        long taskId,
        string approverId,
        string? comment);

    Task ResubmitAsync(
        long taskId,
        string applicantId,
        string? comment);
           // 取得最近一次主管簽核工作
   
}
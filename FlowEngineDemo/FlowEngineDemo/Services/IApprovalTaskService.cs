using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services;

public interface IApprovalTaskService
{
    Task AddAsync(ApprovalTask task);

    // 依 TaskId 取得待簽工作
    Task<ApprovalTask?> GetByTaskIdAsync(long taskId);

    Task<List<ApprovalTask>> GetPendingByApproverIdAsync(
        string approverId);

    Task CompleteAsync(
        long taskId,
        string status);
     Task<ApprovalTask?> GetLatestManagerTaskAsync(
        string flowInstanceId);
}

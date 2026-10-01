using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services;

public interface IApprovalHistoryService
{
    Task AddAsync(ApprovalHistory history);

    Task<List<ApprovalHistory>> GetByFlowInstanceIdAsync(
        string flowInstanceId);
    Task<ApprovalHistory?> GetSubmitHistoryAsync(
        string flowInstanceId);
}
using FlowEngineDemo.Data;
using FlowEngineDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowEngineDemo.Services;

public class ApprovalTaskService : IApprovalTaskService
{
    private readonly FlowEngineDbContext _context;

    public ApprovalTaskService(
        FlowEngineDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ApprovalTask task)
    {
        _context.ApprovalTasks.Add(task);

        await _context.SaveChangesAsync();
    }

    public async Task<List<ApprovalTask>>
        GetPendingByApproverIdAsync(
            string approverId)
    {
        return await _context.ApprovalTasks
            .Where(x =>
                x.ApproverId == approverId &&
                x.Status == "PENDING")
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }
    public async Task CompleteAsync(
          long taskId,
    string status)
    {
        var task = await _context.ApprovalTasks
        .FirstOrDefaultAsync(x =>
            x.Id == taskId &&
            x.Status == "PENDING");

        if (task == null)
        {
            throw new InvalidOperationException(
                $"找不到待簽工作，或此工作已完成。TaskId={taskId}");
        }

            task.Status = status;
            task.CompletedAt = DateTimeOffset.UtcNow;

   

        await _context.SaveChangesAsync();
    }
    public async Task<ApprovalTask?> GetByTaskIdAsync(
        long taskId)
    {
        return await _context.ApprovalTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == taskId);
    }
    public async Task<ApprovalTask?> GetLatestManagerTaskAsync(
            string flowInstanceId)
        {
            return await _context.ApprovalTasks
                .AsNoTracking()
                .Where(x =>
                    x.FlowInstanceId == flowInstanceId &&
                    x.StepCode == "MANAGER_APPROVAL")
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }
}
using FlowEngineDemo.Data;
using FlowEngineDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowEngineDemo.Services;

public class ApprovalHistoryService : IApprovalHistoryService
{
    private readonly FlowEngineDbContext _context;

    public ApprovalHistoryService(
        FlowEngineDbContext context)
    {
        _context = context;
    }
    public async Task AddAsync(ApprovalHistory history)
    {
        // 下一步接 SQL Server
        _context.ApprovalHistories.Add(history);

        await _context.SaveChangesAsync();
        
    }

    public async Task<List<ApprovalHistory>> GetByFlowInstanceIdAsync(
        string flowInstanceId)
    {
        // 下一步接 SQL Server
       return await _context.ApprovalHistories
            .Where(x =>
                x.FlowInstanceId == flowInstanceId)
            .OrderBy(x => x.ActionAt)
            .ToListAsync();
    }
    public async Task<ApprovalHistory?> GetSubmitHistoryAsync(
        string flowInstanceId)
    {
        return await _context.ApprovalHistories
            .Where(x =>
                x.FlowInstanceId == flowInstanceId &&
                x.Action == "SUBMIT")
            .OrderBy(x => x.ActionAt)
            .FirstOrDefaultAsync();
    }
}
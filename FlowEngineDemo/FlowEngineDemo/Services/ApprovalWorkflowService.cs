using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Stores;
using FlowEngineDemo.Models;
namespace FlowEngineDemo.Services;

public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly IWorkflowRuntime _workflowRuntime;
     private readonly IBookmarkStore _bookmarkStore;
    private readonly IApprovalHistoryService _approvalHistoryService;
    public ApprovalWorkflowService(
        IWorkflowRuntime workflowRuntime,
        IBookmarkStore bookmarkStore,
        IApprovalHistoryService approvalHistoryService)
    {
        _workflowRuntime = workflowRuntime;
        _bookmarkStore = bookmarkStore;
        _approvalHistoryService = approvalHistoryService;
    }

    public async Task ApproveAsync(
    string flowInstanceId,
    string approverId,
    string? comment)
    {
        await ResumeApprovalAsync(
            flowInstanceId,
            "APPROVE",
            approverId,
            comment);
    }
    public async Task RejectAsync(
        string flowInstanceId,
        string approverId,
        string? comment)
    {
        await ResumeApprovalAsync(
            flowInstanceId,
            "REJECT",
            approverId,
            comment);
    }
    private async Task ResumeApprovalAsync(
        string flowInstanceId,
        string decision,
        string approverId,
        string? comment)
    {
        var bookmarks = await _bookmarkStore.FindManyAsync(
            new BookmarkFilter
            {
                WorkflowInstanceId = flowInstanceId
            });

        var bookmark = bookmarks.FirstOrDefault(x =>
            x.Payload?.ToString() == "APPROVAL");

        if (bookmark == null)
        {
            throw new InvalidOperationException(
                $"找不到等待簽核的 Bookmark。FlowInstanceId={flowInstanceId}");
        }

        var client = await _workflowRuntime.CreateClientAsync(
            flowInstanceId);

        await client.RunInstanceAsync(
            new RunWorkflowInstanceRequest
            {
                BookmarkId = bookmark.Id,

                Input = new Dictionary<string, object>
                {
                    ["Decision"] = decision,
                    ["ApproverId"] = approverId,
                    ["Comment"] = comment ?? string.Empty
                }
            });

            // Elsa Resume 成功後，記錄簽核歷程
        var submitHistory =
            await _approvalHistoryService.GetSubmitHistoryAsync(
                flowInstanceId);

        if (submitHistory == null)
        {
            throw new InvalidOperationException(
                $"找不到送件紀錄。FlowInstanceId={flowInstanceId}");
        }
        await _approvalHistoryService.AddAsync(
            new ApprovalHistory
            {
                FlowInstanceId = flowInstanceId,
                FormType = submitHistory.FormType,
                FormId = submitHistory.FormId,
                Action = decision,
                ApproverId = approverId,
                Comment = comment,
                ActionAt = DateTimeOffset.UtcNow
            });
    }  
}
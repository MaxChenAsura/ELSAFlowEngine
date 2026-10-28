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
    private readonly IApprovalTaskService _approvalTaskService;
    
    public ApprovalWorkflowService(
        IWorkflowRuntime workflowRuntime,
        IBookmarkStore bookmarkStore,
        IApprovalHistoryService approvalHistoryService,
        IApprovalTaskService approvalTaskService)
    {
        _workflowRuntime = workflowRuntime;
        _bookmarkStore = bookmarkStore;
        _approvalHistoryService = approvalHistoryService;
        _approvalTaskService = approvalTaskService;
    }

    public async Task ApproveAsync(
     long taskId,
    string approverId,
    string? comment)
    {
        await ResumeApprovalAsync(
            taskId,
            "APPROVE",
            approverId,
            comment);
    }
    public async Task RejectAsync(
        long taskId,
        string approverId,
        string? comment)
    {
        await ResumeApprovalAsync(
            taskId,
            "REJECT",
            approverId,
            comment);
    }
    private async Task ResumeApprovalAsync(
        long taskId,
        string decision,
        string approverId,
        string? comment)
    {
        // 1. 依 TaskId 取得待簽工作
        var task = await _approvalTaskService
            .GetByTaskIdAsync(taskId);

        if (task == null)
        {
            throw new InvalidOperationException(
                $"找不到簽核工作。TaskId={taskId}");
        }

        // 2. 確認簽核人員
        if (task.ApproverId != approverId)
        {
            throw new InvalidOperationException(
                $"簽核人員不符。TaskId={taskId}");
        }

        // 3. 確認 Task 尚未完成
        if (task.Status != "PENDING")
        {
            throw new InvalidOperationException(
                $"此簽核工作已處理。TaskId={taskId}, Status={task.Status}");
        }

        // 4. 確認 BookmarkId 存在
        if (string.IsNullOrWhiteSpace(task.BookmarkId))
        {
            throw new InvalidOperationException(
                $"找不到 BookmarkId。TaskId={taskId}");
        }

        // 5. 取得 Elsa Workflow Client
        var client = await _workflowRuntime.CreateClientAsync(
            task.FlowInstanceId);

        // 6. Resume Elsa
        await client.RunInstanceAsync(
            new RunWorkflowInstanceRequest
            {
                BookmarkId = task.BookmarkId,

                Input = new Dictionary<string, object>
                {
                    ["Decision"] = decision,
                    ["ApproverId"] = approverId,
                    ["Comment"] = comment ?? string.Empty
                }
            });

        // 7. 更新 Task 狀態
        await _approvalTaskService.CompleteAsync(
              task.Id,
            decision == "APPROVE"
                ? "APPROVED"
                : "REJECTED");

        // 8. 寫入簽核歷程
        await _approvalHistoryService.AddAsync(
            new ApprovalHistory
            {
                FlowInstanceId = task.FlowInstanceId,

                FormType = task.FormType,
                FormId = task.FormId,

                Action = decision,
                ApproverId = approverId,
                Comment = comment,

                ActionAt = DateTimeOffset.UtcNow
            });

            if (decision == "REJECT")
                {
                    await CreateRevisionTaskAsync(task);
                }
    }
    private async Task CreateRevisionTaskAsync(
        ApprovalTask managerTask)
        {
        // 1. 查詢原始送出紀錄
        var submitHistory =
            await _approvalHistoryService.GetSubmitHistoryAsync(
                managerTask.FlowInstanceId);

        if (submitHistory == null)
        {
            throw new InvalidOperationException(
                $"找不到原始送出紀錄。FlowInstanceId={managerTask.FlowInstanceId}");
        }

        var applicantId = submitHistory.ApproverId;

        // 2. 查詢 Elsa 新建立的 Bookmark
        var bookmarks = await _bookmarkStore.FindManyAsync(
            new BookmarkFilter
            {
                WorkflowInstanceId = managerTask.FlowInstanceId
            });

        var revisionBookmark = bookmarks.FirstOrDefault(
            x => x.Payload?.ToString() == "REVISION");

        if (revisionBookmark == null)
        {
            throw new InvalidOperationException(
                $"找不到 REVISION Bookmark。FlowInstanceId={managerTask.FlowInstanceId}");
        }

        // 3. 建立申請人待辦 Task
        var revisionTask = new ApprovalTask
        {
            FlowInstanceId = managerTask.FlowInstanceId,
            BookmarkId = revisionBookmark.Id,

            FormType = managerTask.FormType,
            FormId = managerTask.FormId,

            StepCode = "APPLICANT_REVISION",
            StepName = "申請人修改",

            ApprovalMode = "SINGLE",
            ApproverId = applicantId,

            Status = "PENDING",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _approvalTaskService.AddAsync(revisionTask);
    }  
    public async Task ResubmitAsync(
        long taskId,
        string applicantId,
        string? comment)
    {
        // 1. 取得申請人的 Task
        var task = await _approvalTaskService.GetByTaskIdAsync(taskId);

        if (task == null)
        {
            throw new InvalidOperationException(
                $"找不到申請人待辦工作。TaskId={taskId}");
        }

        // 2. 確認這是申請人修改關卡
        if (task.StepCode != "APPLICANT_REVISION")
        {
            throw new InvalidOperationException(
                $"此工作不是申請人修改關卡。TaskId={taskId}");
        }

        // 3. 驗證申請人
        if (task.ApproverId != applicantId)
        {
            throw new InvalidOperationException(
                $"申請人不符。TaskId={taskId}");
        }

        // 4. 確認尚未處理
        if (task.Status != "PENDING")
        {
            throw new InvalidOperationException(
                $"此工作已處理。TaskId={taskId}");
        }

        // 5. 確認 BookmarkId
        if (string.IsNullOrWhiteSpace(task.BookmarkId))
        {
            throw new InvalidOperationException(
                $"找不到 REVISION BookmarkId。TaskId={taskId}");
        }

        // 6. 恢復 Elsa Workflow
        var client = await _workflowRuntime.CreateClientAsync(
            task.FlowInstanceId);

        await client.RunInstanceAsync(
            new RunWorkflowInstanceRequest
            {
                BookmarkId = task.BookmarkId,

                Input = new Dictionary<string, object>
                {
                    ["ApplicantId"] = applicantId,
                    ["Comment"] = comment ?? string.Empty,
                    ["Action"] = "RESUBMIT"
                }
            });

        // 7. 更新申請人 Task
        await _approvalTaskService.CompleteAsync(
            task.Id,
            "COMPLETED");

        // 8. 寫入 History
        await _approvalHistoryService.AddAsync(
            new ApprovalHistory
            {
                FlowInstanceId = task.FlowInstanceId,
                FormType = task.FormType,
                FormId = task.FormId,
                Action = "RESUBMIT",
                ApproverId = applicantId,
                Comment = comment,
                ActionAt = DateTimeOffset.UtcNow
            });

        await CreateManagerTaskAsync(task);
    }
    private async Task CreateManagerTaskAsync(
            ApprovalTask revisionTask)
        {
            // 1. 取得這個 Workflow 的歷史 Task
            // 這裡先從原本的主管 Task 找回主管資訊

            var managerTask =
                await _approvalTaskService.GetLatestManagerTaskAsync(
                    revisionTask.FlowInstanceId);

            if (managerTask == null)
            {
                throw new InvalidOperationException(
                    $"找不到原本的主管簽核工作。FlowInstanceId={revisionTask.FlowInstanceId}");
            }

            // 2. 取得 Elsa 新產生的 APPROVAL Bookmark
            var bookmarks = await _bookmarkStore.FindManyAsync(
                new BookmarkFilter
                {
                    WorkflowInstanceId = revisionTask.FlowInstanceId
                });

            var approvalBookmark = bookmarks.FirstOrDefault(
                x => x.Payload?.ToString() == "APPROVAL");

            if (approvalBookmark == null)
            {
                throw new InvalidOperationException(
                    $"找不到新的 APPROVAL Bookmark。FlowInstanceId={revisionTask.FlowInstanceId}");
            }

            // 3. 建立新的主管 Task
            var newManagerTask = new ApprovalTask
            {
                FlowInstanceId = revisionTask.FlowInstanceId,
                BookmarkId = approvalBookmark.Id,

                FormType = revisionTask.FormType,
                FormId = revisionTask.FormId,

                StepCode = "MANAGER_APPROVAL",
                StepName = "主管簽核",

                ApprovalMode = managerTask.ApprovalMode,
                ApproverId = managerTask.ApproverId,

                Status = "PENDING",
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _approvalTaskService.AddAsync(newManagerTask);
        }
}
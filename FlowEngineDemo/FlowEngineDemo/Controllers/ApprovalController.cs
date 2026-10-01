using FlowEngineDemo.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.AspNetCore.Mvc;
using FlowEngineDemo.Services;

namespace FlowEngineDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalController : ControllerBase
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IApprovalHistoryService _approvalHistoryService;

    public ApprovalController(
        IWorkflowRuntime workflowRuntime,
        IApprovalWorkflowService approvalWorkflowService,
        IApprovalHistoryService approvalHistoryService)
    {
        _workflowRuntime = workflowRuntime;
        _approvalWorkflowService = approvalWorkflowService;
        _approvalHistoryService = approvalHistoryService;
    }
    [HttpPost("submit")]
    public async Task<ActionResult<SubmitApprovalResponse>> Submit(
        [FromBody] SubmitApprovalRequest request)
    {
        // 暫時先產生我們自己的 FlowInstanceId。
        // 下一階段會改成真正啟動 Elsa Workflow。
        /**
        var flowInstanceId = Guid.NewGuid().ToString();

        var response = new SubmitApprovalResponse
        {
            FlowInstanceId = flowInstanceId,
            FormType = request.FormType,
            FormId = request.FormId,
            Status = "SUBMITTED"
        };**/
         // 建立 Elsa Workflow Runtime Client
        var client = await _workflowRuntime.CreateClientAsync();

       await client.CreateInstanceAsync(
        new CreateWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle =
                WorkflowDefinitionHandle.ByDefinitionId( request.FlowCode),

            CorrelationId = $"{request.FormType}:{request.FormId}",

            Input = new Dictionary<string, object>
            {
                ["FormType"] = request.FormType,
                ["FormId"] = request.FormId,
                ["ApplicantId"] = request.ApplicantId,
                ["FlowCode"] = request.FlowCode,
                ["Subject"] = request.Subject ?? string.Empty
            }
        });

    // 真正執行 Workflow
    await client.RunInstanceAsync();

    var response = new SubmitApprovalResponse
    {
        // Elsa 真正的 Workflow Instance ID
        FlowInstanceId = client.WorkflowInstanceId,
        FormType = request.FormType,
        FormId = request.FormId,
        Status = "SUBMITTED"
    };
    await _approvalHistoryService.AddAsync(
        new ApprovalHistory
        {
            FlowInstanceId = client.WorkflowInstanceId,
            FormType = request.FormType,
            FormId = request.FormId,
            Action = "SUBMIT",
            ApproverId = request.ApplicantId,
            Comment = request.Subject,
            ActionAt = DateTimeOffset.UtcNow
        });
        return Ok(response);
    }
   [HttpPost("approve")]
    public async Task<IActionResult> Approve(
        [FromBody] ApproveApprovalRequest request)
    {
     

        // 從指定 Bookmark 恢復 Workflow
         await _approvalWorkflowService.ApproveAsync(
        request.FlowInstanceId,
        request.ApproverId,
        request.Comment);

        return Ok(new
            {
                request.FlowInstanceId,
                Decision = "APPROVE",
                request.ApproverId,
                request.Comment
            });
    }
    [HttpPost("reject")]
    public async Task<IActionResult> Reject(
        [FromBody] RejectApprovalRequest request)
    {
        await _approvalWorkflowService.RejectAsync(
            request.FlowInstanceId,
            request.ApproverId,
            request.Comment);

        return Ok(new
        {
            request.FlowInstanceId,
            Decision = "REJECT",
            request.ApproverId,
            request.Comment
        });
    }
}
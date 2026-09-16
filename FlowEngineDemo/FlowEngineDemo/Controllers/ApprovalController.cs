using FlowEngineDemo.Models;
using Microsoft.AspNetCore.Mvc;

namespace FlowEngineDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalController : ControllerBase
{
    [HttpPost("submit")]
    public ActionResult<SubmitApprovalResponse> Submit(
        [FromBody] SubmitApprovalRequest request)
    {
        // 暫時先產生我們自己的 FlowInstanceId。
        // 下一階段會改成真正啟動 Elsa Workflow。
        var flowInstanceId = Guid.NewGuid().ToString();

        var response = new SubmitApprovalResponse
        {
            FlowInstanceId = flowInstanceId,
            FormType = request.FormType,
            FormId = request.FormId,
            Status = "SUBMITTED"
        };

        return Ok(response);
    }
}
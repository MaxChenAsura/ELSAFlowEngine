using FlowEngineDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowEngineDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlowDefinitionTestController : ControllerBase
    {
        private readonly IFlowDefinitionService _flowDefinitionService;
        private readonly IFlowStepService _flowStepService;

        public FlowDefinitionTestController(
            IFlowDefinitionService flowDefinitionService,
            IFlowStepService flowStepService)
        {
            _flowDefinitionService = flowDefinitionService;
            _flowStepService = flowStepService;
        }

        /// <summary>
        /// 測試讀取 DB 流程設定
        /// </summary>
        [HttpGet("{flowCode}")]
        public async Task<IActionResult> GetFlow(string flowCode)
        {
            var flow = await _flowDefinitionService
                .GetActiveFlowAsync(flowCode);

            if (flow == null)
            {
                return NotFound(new
                {
                    Message = $"找不到啟用中的流程：{flowCode}"
                });
            }

            return Ok(flow);
        }

        /// <summary>
        /// 測試根據 DB 設定取得下一個流程關卡
        /// </summary>
        [HttpGet("next-step")]
        public async Task<IActionResult> GetNextStep(
            [FromQuery] string flowCode,
            [FromQuery] int versionNo,
            [FromQuery] string currentStepCode,
            [FromQuery] string resultCode)
        {
            var nextStep = await _flowStepService.GetNextStepAsync(
                flowCode,
                versionNo,
                currentStepCode,
                resultCode);

            if (nextStep == null)
            {
                return NotFound(new
                {
                    Message = "找不到下一個關卡"
                });
            }

            return Ok(new
            {
                FlowCode = flowCode,
                VersionNo = versionNo,
                CurrentStepCode = currentStepCode,
                ResultCode = resultCode,
                NextStepCode = nextStep.StepCode,
                NextStepName = nextStep.StepName,
                NextStepType = nextStep.StepType,
                ApprovalMode = nextStep.ApprovalMode,
                Approvers = nextStep.Approvers
            });
        }
        [HttpGet("validate-transition")]
            public async Task<IActionResult> ValidateTransition(
                [FromQuery] string flowCode,
                [FromQuery] int versionNo,
                [FromQuery] string currentStepCode,
                [FromQuery] string resultCode,
                [FromQuery] string nextAction,
                [FromQuery] string? nextStepCode)
            {
                var result = await _flowStepService.ValidateTransitionAsync(
                    flowCode,
                    versionNo,
                    currentStepCode,
                    resultCode,
                    nextAction,
                    nextStepCode);

                if (!result.IsValid)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
    }
}
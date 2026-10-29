using Microsoft.AspNetCore.Mvc;
using FlowEngineDemo.Services;
using FlowEngineDemo.Models;
using System.Text.Json;

namespace FlowEngineDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParameterTestController : ControllerBase
    {
        private readonly IParameterService _parameterService;

        public ParameterTestController(
            IParameterService parameterService)
        {
            _parameterService = parameterService;
        }

        // 第七步：取得參數定義
        [HttpGet("definitions")]
        public async Task<IActionResult> GetDefinitions(
            [FromQuery] string flowCode,
            [FromQuery] int versionNo)
        {
            var result =
                await _parameterService.GetParameterDefinitionsAsync(
                    flowCode,
                    versionNo);

            return Ok(result);
        }

        // 第八步：驗證參數
        [HttpPost("validate")]
        public async Task<IActionResult> Validate(
            [FromBody] ValidateParameterRequest request)
        {
            var result =
                await _parameterService.ValidateParametersAsync(
                    request.FlowCode,
                    request.VersionNo,
                    request.Parameters);

            return Ok(result);
        }

        // 第九步：儲存流程實例參數
        [HttpPost("save")]
        public async Task<IActionResult> Save(
            [FromBody] SaveInstanceParametersRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.FlowInstanceId))
            {
                return BadRequest("FlowInstanceId 不可為空。");
            }

            try
            {
                await _parameterService.SaveInstanceParametersAsync(
                    request.FlowInstanceId,
                    request.Parameters);

                return Ok(new
                {
                    success = true,
                    flowInstanceId = request.FlowInstanceId,
                    message = "流程實例參數儲存成功。"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet("instance")]
        public async Task<IActionResult> GetInstanceParameters(
            [FromQuery] string flowInstanceId)
        {
            if (string.IsNullOrWhiteSpace(flowInstanceId))
            {
                return BadRequest("FlowInstanceId 不可為空。");
            }

            var result =
                await _parameterService.GetInstanceParametersAsync(
                    flowInstanceId);

            return Ok(new
            {
                flowInstanceId,
                parameters = result
            });
        }
        [HttpPost("update")]
        public async Task<IActionResult> Update(
            [FromBody] SaveInstanceParametersRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.FlowInstanceId))
            {
                return BadRequest("FlowInstanceId 不可為空。");
            }

            try
            {
                await _parameterService.UpdateInstanceParametersAsync(
                    request.FlowInstanceId,
                    request.Parameters);

                return Ok(new
                {
                    success = true,
                    flowInstanceId = request.FlowInstanceId,
                    message = "流程實例參數更新成功。"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }

    // Request Model 必須放在 Controller 類別外面
    public class ValidateParameterRequest
    {
        public string FlowCode { get; set; } = string.Empty;

        public int VersionNo { get; set; }

        public Dictionary<string, JsonElement> Parameters { get; set; }
            = new();
    }
}
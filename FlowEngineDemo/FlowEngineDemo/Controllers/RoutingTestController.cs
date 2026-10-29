using Microsoft.AspNetCore.Mvc;
using FlowEngineDemo.Models;
using FlowEngineDemo.Services;

namespace FlowEngineDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoutingTestController : ControllerBase
    {
        private readonly IRoutingService _routingService;

        public RoutingTestController(
            IRoutingService routingService)
        {
            _routingService = routingService;
        }

        [HttpPost("resolve")]
        public async Task<IActionResult> Resolve(
            [FromBody] RoutingRequest request)
        {
            try
            {
                var result =
                    await _routingService.ResolveNextStepAsync(request);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
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
}
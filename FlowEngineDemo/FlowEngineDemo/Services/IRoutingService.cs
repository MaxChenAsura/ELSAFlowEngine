using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services
{
    public interface IRoutingService
    {
        Task<RoutingResult> ResolveNextStepAsync(
            RoutingRequest request);
    }
}
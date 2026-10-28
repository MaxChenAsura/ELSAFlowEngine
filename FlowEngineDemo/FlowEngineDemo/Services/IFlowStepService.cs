using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services
{
    public interface IFlowStepService
    {
        /// <summary>
        /// 根據流程版本、目前關卡及處理結果，
        /// 取得下一個關卡。
        /// </summary>
        Task<FlowStepModel?> GetNextStepAsync(
            string flowCode,
            int versionNo,
            string currentStepCode,
            string resultCode);
        Task<FlowTransitionValidationResult> ValidateTransitionAsync(
            string flowCode,
            int versionNo,
            string currentStepCode,
            string resultCode,
            string nextAction,
            string? nextStepCode);
    }
}
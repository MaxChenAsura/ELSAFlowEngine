using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services
{
    public class FlowStepService : IFlowStepService
    {
        private readonly IFlowDefinitionService _flowDefinitionService;

        public FlowStepService(
            IFlowDefinitionService flowDefinitionService)
        {
            _flowDefinitionService = flowDefinitionService;
        }

        public async Task<FlowStepModel?> GetNextStepAsync(
            string flowCode,
            int versionNo,
            string currentStepCode,
            string resultCode)
        {
            // 1. 讀取指定版本的流程設定
            var flow = await _flowDefinitionService
                .GetFlowVersionAsync(flowCode, versionNo);

            if (flow == null)
            {
                throw new InvalidOperationException(
                    $"找不到流程設定：{flowCode}，Version={versionNo}");
            }

            // 2. 找出目前關卡
            var currentStep = flow.Steps.FirstOrDefault(
                x => x.StepCode == currentStepCode);

            if (currentStep == null)
            {
                throw new InvalidOperationException(
                    $"找不到關卡：{currentStepCode}");
            }

            // 3. 根據處理結果取得轉換規則
            var transitions = currentStep.Transitions
                .Where(x => x.ResultCode == resultCode)
                .OrderBy(x => x.Priority)
                .ToList();

            if (transitions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"關卡 {currentStepCode} 找不到處理結果 {resultCode} 的轉換規則");
            }

            // 4. 目前只支援無條件轉換
            var unconditionalTransitions = transitions
                .Where(x => string.IsNullOrWhiteSpace(x.ConditionCode))
                .ToList();

            if (transitions.Count != 1 ||
                unconditionalTransitions.Count != 1)
            {
                throw new NotSupportedException(
                    $"關卡 {currentStepCode} 的轉換規則包含條件或多條分支，" +
                    "目前尚未實作條件解析，不能自動選擇下一關。");
            }

            var transition = unconditionalTransitions[0];

            // 5. 取得下一個關卡
            var nextStep = flow.Steps.FirstOrDefault(
                x => x.StepCode == transition.ToStepCode);

            if (nextStep == null)
            {
                throw new InvalidOperationException(
                    $"找不到下一個關卡：{transition.ToStepCode}");
            }

            return nextStep;
        }
        public async Task<FlowTransitionValidationResult> ValidateTransitionAsync(
            string flowCode,
            int versionNo,
            string currentStepCode,
            string resultCode,
            string nextAction,
            string? nextStepCode)
        {
            FlowTransitionValidationResult Invalid(
                string code,
                string message)
            {
                return new FlowTransitionValidationResult
                {
                    IsValid = false,
                    ErrorCode = code,
                    ErrorMessage = message
                };
            }

            if (string.IsNullOrWhiteSpace(flowCode) ||
                string.IsNullOrWhiteSpace(currentStepCode) ||
                string.IsNullOrWhiteSpace(resultCode) ||
                string.IsNullOrWhiteSpace(nextAction) ||
                versionNo <= 0)
            {
                return Invalid(
                    "INVALID_PARAMETER",
                    "流程代碼、版本、目前關卡、結果及下一步動作不可為空。");
            }

            flowCode = flowCode.Trim();
            currentStepCode = currentStepCode.Trim();
            resultCode = resultCode.Trim().ToUpperInvariant();
            nextAction = nextAction.Trim().ToUpperInvariant();
            nextStepCode = nextStepCode?.Trim();

            if (nextAction != "NEXT" &&
                nextAction != "REPEAT" &&
                nextAction != "FINISH")
            {
                return Invalid(
                    "INVALID_NEXT_ACTION",
                    $"不支援的 NextAction：{nextAction}");
            }

            // 取得指定版本，而不是直接取得最新 ACTIVE 版本。
            // 已啟動的 Workflow 必須固定使用原始版本。
            var flow = await _flowDefinitionService.GetFlowVersionAsync(
                flowCode,
                versionNo);

            if (flow == null)
            {
                return Invalid(
                    "FLOW_NOT_FOUND",
                    $"找不到流程 {flowCode}，版本 {versionNo}。");
            }

            var currentStep = flow.Steps.FirstOrDefault(
                x => x.StepCode == currentStepCode);

            if (currentStep == null)
            {
                return Invalid(
                    "CURRENT_STEP_NOT_FOUND",
                    $"找不到目前關卡：{currentStepCode}");
            }

            if (currentStep.StepType == "END")
            {
                return Invalid(
                    "CURRENT_STEP_IS_END",
                    "END 關卡不能再執行關卡轉換。");
            }

            // REPEAT：外部系統要求同一關卡再執行一次。
            if (nextAction == "REPEAT")
            {
                if (resultCode != "APPROVE")
                {
                    return Invalid(
                        "INVALID_REPEAT_RESULT",
                        "目前僅允許 APPROVE 後執行 REPEAT。");
                }

                if (currentStep.StepType != "APPROVAL")
                {
                    return Invalid(
                        "INVALID_REPEAT_STEP",
                        "目前僅允許 APPROVAL 關卡執行 REPEAT。");
                }

                if (!string.IsNullOrEmpty(nextStepCode) &&
                    nextStepCode != currentStepCode)
                {
                    return Invalid(
                        "INVALID_REPEAT_TARGET",
                        "REPEAT 必須維持在目前關卡。");
                }

                // 注意：
                // 這裡還不能直接允許 REPEAT。
                // 必須先確認流程定義明確允許自我循環。
                var repeatTransition = currentStep.Transitions.FirstOrDefault(
                    x => x.FromStepCode == currentStepCode &&
                        x.ToStepCode == currentStepCode &&
                        x.ResultCode == resultCode &&
                        string.IsNullOrEmpty(x.ConditionCode));

                if (repeatTransition == null)
                {
                    return Invalid(
                        "REPEAT_NOT_ALLOWED",
                        $"流程未設定 {currentStepCode} 的 REPEAT 轉換。");
                }

                return new FlowTransitionValidationResult
                {
                    IsValid = true,
                    NextStepCode = currentStepCode,
                    NextStep = currentStep
                };
            }

            // NEXT / FINISH 必須指定目標關卡。
            if (string.IsNullOrWhiteSpace(nextStepCode))
            {
                return Invalid(
                    "NEXT_STEP_REQUIRED",
                    "NEXT 或 FINISH 必須指定 NextStepCode。");
            }

            var targetStep = flow.Steps.FirstOrDefault(
                x => x.StepCode == nextStepCode);

            if (targetStep == null)
            {
                return Invalid(
                    "NEXT_STEP_NOT_FOUND",
                    $"找不到下一關：{nextStepCode}");
            }

            if (nextAction == "FINISH" && targetStep.StepType != "END")
            {
                return Invalid(
                    "INVALID_FINISH_TARGET",
                    "FINISH 必須指向 END 關卡。");
            }

            if (nextAction == "NEXT" && targetStep.StepType == "END")
            {
                return Invalid(
                    "INVALID_NEXT_TARGET",
                    "END 關卡必須使用 FINISH。");
            }

            // 只允許資料庫定義的合法轉換。
            var transition = currentStep.Transitions.FirstOrDefault(
                x => x.FromStepCode == currentStepCode &&
                    x.ToStepCode == nextStepCode &&
                    x.ResultCode == resultCode &&
                    string.IsNullOrEmpty(x.ConditionCode));

            if (transition == null)
            {
                return Invalid(
                    "TRANSITION_NOT_ALLOWED",
                    $"流程不允許 {currentStepCode} " +
                    $"透過 {resultCode} 轉換到 {nextStepCode}。");
            }

            return new FlowTransitionValidationResult
            {
                IsValid = true,
                NextStepCode = nextStepCode,
                NextStep = targetStep
            };
        }
    }
}
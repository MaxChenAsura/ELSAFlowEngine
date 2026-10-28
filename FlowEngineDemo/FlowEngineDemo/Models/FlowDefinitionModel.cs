using System;
using System.Collections.Generic;

namespace FlowEngineDemo.Models
{
    /// <summary>
    /// 通用簽核流程定義
    /// </summary>
    public class FlowDefinitionModel
    {
        public string FlowCode { get; set; } = string.Empty;

        public string FlowName { get; set; } = string.Empty;

        public string FormType { get; set; } = string.Empty;

        public int VersionNo { get; set; }

        public string StartStepCode { get; set; } = string.Empty;

        public List<FlowStepModel> Steps { get; set; } = new();
    }

    /// <summary>
    /// 流程關卡
    /// </summary>
    public class FlowStepModel
    {
        public string StepCode { get; set; } = string.Empty;

        public string StepName { get; set; } = string.Empty;

        public string StepType { get; set; } = string.Empty;

        public string? ApprovalMode { get; set; }

        public int SortNo { get; set; }

        public List<FlowStepApproverModel> Approvers { get; set; } = new();

        public List<FlowTransitionModel> Transitions { get; set; } = new();
    }

    /// <summary>
    /// 關卡處理人員設定
    /// </summary>
    public class FlowStepApproverModel
    {
        public string ApproverType { get; set; } = string.Empty;

        public string? ApproverValue { get; set; }
    }

    /// <summary>
    /// 流程轉換規則
    /// </summary>
    public class FlowTransitionModel
    {
        public string FromStepCode { get; set; } = string.Empty;

        public string ToStepCode { get; set; } = string.Empty;

        public string ResultCode { get; set; } = string.Empty;

        public string? ConditionCode { get; set; }

        public int Priority { get; set; }
    }
}
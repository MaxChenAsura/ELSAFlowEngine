using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services
{
    /// <summary>
    /// 通用簽核人員解析服務
    /// </summary>
    public interface IFlowApproverResolverService
    {
        /// <summary>
        /// 根據流程關卡設定，解析實際處理人員
        /// </summary>
        /// <param name="context">流程與表單資訊</param>
        /// <returns>實際處理人員的員工編號集合</returns>
        Task<List<string>> ResolveApproversAsync(
            FlowApproverResolveContext context);
    }
}
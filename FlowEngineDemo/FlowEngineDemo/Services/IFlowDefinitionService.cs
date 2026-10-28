using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services
{
    /// <summary>
    /// 通用簽核流程定義服務
    /// </summary>
    public interface IFlowDefinitionService
    {
        /// <summary>
        /// 取得指定流程目前啟用的版本及完整關卡設定
        /// </summary>
        /// <param name="flowCode">流程代碼，例如 PURCHASE、LEAVE</param>
        /// <returns>流程定義；不存在時回傳 null</returns>
        Task<FlowDefinitionModel?> GetActiveFlowAsync(string flowCode);

        /// <summary>
        /// 取得指定流程的特定版本
        /// </summary>
        /// <param name="flowCode">流程代碼</param>
        /// <param name="versionNo">流程版本</param>
        /// <returns>流程定義；不存在時回傳 null</returns>
        Task<FlowDefinitionModel?> GetFlowVersionAsync(
            string flowCode,
            int versionNo);
    }
}
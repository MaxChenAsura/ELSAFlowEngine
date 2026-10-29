using System.Text.Json;
using FlowEngineDemo.Models;

namespace FlowEngineDemo.Services
{
    public interface IParameterService
    {
        Task<List<FlowParameterDefinitionModel>>
            GetParameterDefinitionsAsync(
                string flowCode,
                int versionNo);

        Task<ParameterValidationResult>
            ValidateParametersAsync(
                string flowCode,
                int versionNo,
                Dictionary<string, JsonElement> parameters);

        Task SaveInstanceParametersAsync(
            string flowInstanceId,
            Dictionary<string, JsonElement> parameters);

        Task<Dictionary<string, string?>> GetInstanceParametersAsync(
             string flowInstanceId);
        Task UpdateInstanceParametersAsync(
        string flowInstanceId,
        Dictionary<string, JsonElement> parameters);
    }
}
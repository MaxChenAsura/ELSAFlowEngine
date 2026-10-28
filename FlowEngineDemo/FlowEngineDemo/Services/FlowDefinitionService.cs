using System.Data;
using System.Data.Common;
using FlowEngineDemo.Models;
using Microsoft.EntityFrameworkCore;
using FlowEngineDemo.Data;

namespace FlowEngineDemo.Services
{
    public class FlowDefinitionService : IFlowDefinitionService
    {
        private readonly FlowEngineDbContext _context;

        public FlowDefinitionService(FlowEngineDbContext context)
        {
            _context = context;
        }

        public async Task<FlowDefinitionModel?> GetActiveFlowAsync(
            string flowCode)
        {
            return await LoadFlowAsync(flowCode, null);
        }

        public async Task<FlowDefinitionModel?> GetFlowVersionAsync(
            string flowCode,
            int versionNo)
        {
            return await LoadFlowAsync(flowCode, versionNo);
        }

        private async Task<FlowDefinitionModel?> LoadFlowAsync(
            string flowCode,
            int? versionNo)
        {
            var connection = _context.Database.GetDbConnection();
            bool shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
                await connection.OpenAsync();

            try
            {
                FlowDefinitionModel? flow;

                // 1. 讀取流程主檔與版本
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT
                            D.FLOW_CODE,
                            D.FLOW_NAME,
                            D.FORM_TYPE,
                            V.VERSION_NO,
                            V.START_STEP_CODE
                        FROM dbo.TB_FLOW_DEF D
                        INNER JOIN dbo.TB_FLOW_VERSION V
                            ON D.FLOW_CODE = V.FLOW_CODE
                        WHERE D.FLOW_CODE = @FLOW_CODE
                          AND (
                              (@VERSION_NO IS NULL
                               AND D.STATUS = 'ACTIVE'
                               AND V.STATUS = 'ACTIVE')
                              OR V.VERSION_NO = @VERSION_NO
                          )";

                    AddParameter(command, "@FLOW_CODE", flowCode);
                    AddParameter(command, "@VERSION_NO", versionNo);

                    await using var reader =
                        await command.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        return null;

                    flow = new FlowDefinitionModel
                    {
                        FlowCode = reader.GetString(0),
                        FlowName = reader.GetString(1),
                        FormType = reader.GetString(2),
                        VersionNo = reader.GetInt32(3),
                        StartStepCode = reader.GetString(4)
                    };
                }

                // 2. 讀取流程關卡
                await using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT
                            STEP_CODE,
                            STEP_NAME,
                            STEP_TYPE,
                            APPROVAL_MODE,
                            SORT_NO
                        FROM dbo.TB_FLOW_STEP
                        WHERE FLOW_CODE = @FLOW_CODE
                          AND VERSION_NO = @VERSION_NO
                        ORDER BY SORT_NO";

                    AddParameter(command, "@FLOW_CODE", flow.FlowCode);
                    AddParameter(command, "@VERSION_NO", flow.VersionNo);

                    await using var reader =
                        await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        flow.Steps.Add(new FlowStepModel
                        {
                            StepCode = reader.GetString(0),
                            StepName = reader.GetString(1),
                            StepType = reader.GetString(2),
                            ApprovalMode = reader.IsDBNull(3)
                                ? null : reader.GetString(3),
                            SortNo = reader.GetInt32(4)
                        });
                    }
                }

                // 3. 讀取每個關卡的處理人員與轉換規則
                foreach (var step in flow.Steps)
                {
                    await LoadApproversAsync(
                        connection, flow, step);

                    await LoadTransitionsAsync(
                        connection, flow, step);
                }

                return flow;
            }
            finally
            {
                if (shouldClose)
                    await connection.CloseAsync();
            }
        }

        private static async Task LoadApproversAsync(
            DbConnection connection,
            FlowDefinitionModel flow,
            FlowStepModel step)
        {
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT APPROVER_TYPE, APPROVER_VALUE
                FROM dbo.TB_FLOW_STEP_APPROVER
                WHERE FLOW_CODE = @FLOW_CODE
                  AND VERSION_NO = @VERSION_NO
                  AND STEP_CODE = @STEP_CODE
                ORDER BY ID";

            AddParameter(command, "@FLOW_CODE", flow.FlowCode);
            AddParameter(command, "@VERSION_NO", flow.VersionNo);
            AddParameter(command, "@STEP_CODE", step.StepCode);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                step.Approvers.Add(new FlowStepApproverModel
                {
                    ApproverType = reader.GetString(0),
                    ApproverValue = reader.IsDBNull(1)
                        ? null : reader.GetString(1)
                });
            }
        }

        private static async Task LoadTransitionsAsync(
            DbConnection connection,
            FlowDefinitionModel flow,
            FlowStepModel step)
        {
            await using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT
                    FROM_STEP_CODE,
                    TO_STEP_CODE,
                    RESULT_CODE,
                    CONDITION_CODE,
                    PRIORITY
                FROM dbo.TB_FLOW_TRANSITION
                WHERE FLOW_CODE = @FLOW_CODE
                  AND VERSION_NO = @VERSION_NO
                  AND FROM_STEP_CODE = @STEP_CODE
                ORDER BY PRIORITY, ID";

            AddParameter(command, "@FLOW_CODE", flow.FlowCode);
            AddParameter(command, "@VERSION_NO", flow.VersionNo);
            AddParameter(command, "@STEP_CODE", step.StepCode);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                step.Transitions.Add(new FlowTransitionModel
                {
                    FromStepCode = reader.GetString(0),
                    ToStepCode = reader.GetString(1),
                    ResultCode = reader.GetString(2),
                    ConditionCode = reader.IsDBNull(3)
                        ? null : reader.GetString(3),
                    Priority = reader.GetInt32(4)
                });
            }
        }

        private static void AddParameter(
            DbCommand command,
            string name,
            object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
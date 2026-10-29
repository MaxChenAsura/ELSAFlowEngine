using System.Data;
using Microsoft.Data.SqlClient;
using FlowEngineDemo.Models;
using System.Globalization;
namespace FlowEngineDemo.Services
{
    public class RoutingService : IRoutingService
    {
        private readonly string _connectionString;

        public RoutingService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("Elsa")
                ?? throw new InvalidOperationException(
                    "找不到 Elsa 資料庫連線字串。");
        }

        public async Task<RoutingResult> ResolveNextStepAsync(
            RoutingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.FlowInstanceId))
                throw new ArgumentException(
                    "FlowInstanceId 不可為空。");

            if (string.IsNullOrWhiteSpace(request.CurrentStepCode))
                throw new ArgumentException(
                    "CurrentStepCode 不可為空。");

            if (string.IsNullOrWhiteSpace(request.ResultCode))
                throw new ArgumentException(
                    "ResultCode 不可為空。");

            await using var conn =
                new SqlConnection(_connectionString);

            await conn.OpenAsync();

            // 1. 取得流程實例及路由模式
            const string sqlInstance = @"
                SELECT
                    I.FLOW_CODE,
                    I.VERSION_NO,
                    I.CURRENT_STEP_CODE,
                    V.ROUTING_MODE
                FROM dbo.TB_FLOW_INSTANCE I
                INNER JOIN dbo.TB_FLOW_VERSION V
                    ON I.FLOW_CODE = V.FLOW_CODE
                   AND I.VERSION_NO = V.VERSION_NO
                WHERE I.FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID;
            ";

            string flowCode;
            int versionNo;
            string currentStepCode;
            string routingMode;

            await using (var cmd =
                new SqlCommand(sqlInstance, conn))
            {
                cmd.Parameters.Add(
                    "@FLOW_INSTANCE_ID",
                    SqlDbType.VarChar,
                    100).Value = request.FlowInstanceId.Trim();

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException(
                        $"找不到流程實例：{request.FlowInstanceId}");
                }

                flowCode = reader.GetString(0);
                versionNo = reader.GetInt32(1);
                currentStepCode = reader.GetString(2);
                routingMode = reader.GetString(3);
            }

            // 2. 驗證目前步驟
            if (!string.Equals(
                currentStepCode,
                request.CurrentStepCode,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"目前步驟不一致，資料庫為 {currentStepCode}，" +
                    $"Request 為 {request.CurrentStepCode}");
            }

            // 3. 取得目前步驟的 OUT 定義
            var outs = await GetStepOutsAsync(
                conn,
                flowCode,
                versionNo,
                currentStepCode,
                request.ResultCode);

            // 4. RULE 模式：根據條件選擇 OUT
            string? selectedOutCode = null;
            string message =
                $"成功取得路由設定，符合 RESULT_CODE 的 OUT 數量：{outs.Count}。";

            if (string.Equals(
                routingMode,
                "RULE",
                StringComparison.OrdinalIgnoreCase))
            {
                if (outs.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"找不到 RULE 路由設定：{currentStepCode}/{request.ResultCode}");
                }

                // 取得流程實例參數
                var parameters = await GetInstanceParametersAsync(
                    conn,
                    request.FlowInstanceId);

                var matchedOuts = new List<FlowStepOutModel>();

                foreach (var stepOut in outs)
                {
                    if (!string.Equals(
                        stepOut.RouteType,
                        "RULE",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var conditions = await GetOutConditionsAsync(
                        conn,
                        flowCode,
                        versionNo,
                        currentStepCode,
                        stepOut.OutCode);

                    // 無條件的 OUT，只有 IS_DEFAULT = true 才符合
                    if (conditions.Count == 0)
                    {
                        if (stepOut.IsDefault)
                            matchedOuts.Add(stepOut);

                        continue;
                    }

                    bool allMatched = true;

                    foreach (var condition in conditions)
                    {
                        if (!parameters.TryGetValue(
                            condition.ParamName,
                            out var actualValue))
                        {
                            throw new InvalidOperationException(
                                $"流程實例缺少參數：{condition.ParamName}");
                        }

                       // 取得參數資料型別
                        string dataType = await GetParameterDataTypeAsync(
                            conn,
                            flowCode,
                            versionNo,
                            condition.ParamName);

                        // 根據 DATA_TYPE 選擇比較方法
                        bool conditionMatched = dataType switch
                        {
                            "DECIMAL" => EvaluateDecimalCondition(
                                actualValue,
                                condition.Operator,
                                condition.CompareValue),

                            "INT" => EvaluateIntCondition(
                                actualValue,
                                condition.Operator,
                                condition.CompareValue),

                            _ => throw new NotSupportedException(
                                $"目前尚未支援參數型別：{dataType}")
                        };

                        if (!conditionMatched)
                        {
                            allMatched = false;
                            break;
                        }
                    }

                    if (allMatched)
                        matchedOuts.Add(stepOut);
                }

                if (matchedOuts.Count > 1)
                {
                    throw new InvalidOperationException(
                        "RULE 路由條件重複符合多個 OUT，請檢查設定。");
                }

                if (matchedOuts.Count == 0)
                {
                    throw new InvalidOperationException(
                        "RULE 路由沒有符合的 OUT，請檢查條件設定。");
                }

                selectedOutCode = matchedOuts[0].OutCode;
                message = $"RULE 路由判斷成功，選擇 OUT：{selectedOutCode}。";
            }
            // 5. 根據選定的 OUT 取得下一步
            string? nextStepCode = null;

            if (!string.IsNullOrWhiteSpace(selectedOutCode))
            {
                nextStepCode = await GetNextStepCodeAsync(
                    conn,
                    flowCode,
                    versionNo,
                    currentStepCode,
                    request.ResultCode,
                    selectedOutCode);
            }
            // 6. 判斷下一個步驟是否為 END
            bool isCompleted = false;

            if (!string.IsNullOrWhiteSpace(nextStepCode))
            {
                isCompleted = await IsEndStepAsync(
                    conn,
                    flowCode,
                    versionNo,
                    nextStepCode);
            }
            // 7. 回傳結果；下一階段才解析 NextStepCode
            return new RoutingResult
            {
                Success = true,
                FlowInstanceId = request.FlowInstanceId,
                FlowCode = flowCode,
                VersionNo = versionNo,
                RoutingMode = routingMode,
                CurrentStepCode = currentStepCode,
                ResultCode = request.ResultCode,
                SelectedOutCode = selectedOutCode,
                NextStepCode = nextStepCode,
                IsCompleted = isCompleted,
                Message = message
            };
        }
        private async Task<bool> IsEndStepAsync(
            SqlConnection conn,
            string flowCode,
            int versionNo,
            string stepCode)
        {
            const string sql = @"
                SELECT STEP_TYPE
                FROM dbo.TB_FLOW_STEP
                WHERE FLOW_CODE = @FLOW_CODE
                AND VERSION_NO = @VERSION_NO
                AND STEP_CODE = @STEP_CODE;
            ";

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@FLOW_CODE", SqlDbType.VarChar, 50)
                .Value = flowCode;

            cmd.Parameters.Add("@VERSION_NO", SqlDbType.Int)
                .Value = versionNo;

            cmd.Parameters.Add("@STEP_CODE", SqlDbType.VarChar, 50)
                .Value = stepCode;

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    $"找不到流程步驟：{flowCode} v{versionNo} / {stepCode}");
            }

            return string.Equals(
                Convert.ToString(result),
                "END",
                StringComparison.OrdinalIgnoreCase);
        }
    private async Task<string> GetNextStepCodeAsync(
        SqlConnection conn,
        string flowCode,
        int versionNo,
        string currentStepCode,
        string resultCode,
        string selectedOutCode)
    {
        const string sql = @"
            SELECT TO_STEP_CODE
            FROM dbo.TB_FLOW_TRANSITION
            WHERE FLOW_CODE = @FLOW_CODE
            AND VERSION_NO = @VERSION_NO
            AND FROM_STEP_CODE = @FROM_STEP_CODE
            AND RESULT_CODE = @RESULT_CODE
            AND OUT_CODE = @OUT_CODE;
        ";

        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add("@FLOW_CODE", SqlDbType.VarChar, 50)
            .Value = flowCode;

        cmd.Parameters.Add("@VERSION_NO", SqlDbType.Int)
            .Value = versionNo;

        cmd.Parameters.Add("@FROM_STEP_CODE", SqlDbType.VarChar, 50)
            .Value = currentStepCode;

        cmd.Parameters.Add("@RESULT_CODE", SqlDbType.VarChar, 30)
            .Value = resultCode;

        cmd.Parameters.Add("@OUT_CODE", SqlDbType.VarChar, 50)
            .Value = selectedOutCode;

        var result = await cmd.ExecuteScalarAsync();

        if (result == null || result == DBNull.Value)
        {
            throw new InvalidOperationException(
                $"找不到流程轉移設定：{currentStepCode} / " +
                $"{resultCode} / {selectedOutCode}");
        }

        return Convert.ToString(result)!;
    }
        private async Task<List<FlowStepOutModel>> GetStepOutsAsync(
                SqlConnection conn,
                string flowCode,
                int versionNo,
                string stepCode,
                string resultCode)
            {
                var result = new List<FlowStepOutModel>();

                const string sql = @"
                    SELECT
                        FLOW_CODE,
                        VERSION_NO,
                        STEP_CODE,
                        OUT_CODE,
                        OUT_NAME,
                        RESULT_CODE,
                        ROUTE_TYPE,
                        PRIORITY,
                        IS_DEFAULT,
                        REMARK
                    FROM dbo.TB_FLOW_STEP_OUT
                    WHERE FLOW_CODE = @FLOW_CODE
                    AND VERSION_NO = @VERSION_NO
                    AND STEP_CODE = @STEP_CODE
                    AND RESULT_CODE = @RESULT_CODE
                    ORDER BY PRIORITY, OUT_CODE;
                ";

                await using var cmd =
                    new SqlCommand(sql, conn);

                cmd.Parameters.Add(
                    "@FLOW_CODE",
                    SqlDbType.VarChar,
                    50).Value = flowCode;

                cmd.Parameters.Add(
                    "@VERSION_NO",
                    SqlDbType.Int).Value = versionNo;

                cmd.Parameters.Add(
                    "@STEP_CODE",
                    SqlDbType.VarChar,
                    50).Value = stepCode;

                cmd.Parameters.Add(
                    "@RESULT_CODE",
                    SqlDbType.VarChar,
                    30).Value = resultCode;

                await using var reader =
                    await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new FlowStepOutModel
                    {
                        FlowCode = reader.GetString(0),
                        VersionNo = reader.GetInt32(1),
                        StepCode = reader.GetString(2),
                        OutCode = reader.GetString(3),
                        OutName = reader.IsDBNull(4)
                            ? null : reader.GetString(4),
                        ResultCode = reader.GetString(5),
                        RouteType = reader.GetString(6),
                        Priority = reader.GetInt32(7),
                        IsDefault = reader.GetBoolean(8),
                        Remark = reader.IsDBNull(9)
                            ? null : reader.GetString(9)
                    });
                }

                return result;
            }
            private bool EvaluateDecimalCondition(
                string? actualValue,
                string op,
                string? compareValue)
            {
                if (!decimal.TryParse(
                    actualValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal actual))
                {
                    throw new InvalidOperationException(
                        $"無法轉換為 DECIMAL：{actualValue}");
                }

                if (!decimal.TryParse(
                    compareValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal expected))
                {
                    throw new InvalidOperationException(
                        $"條件比較值無法轉換為 DECIMAL：{compareValue}");
                }

                return op.ToUpperInvariant() switch
                {
                    "EQ" => actual == expected,
                    "NE" => actual != expected,
                    "GT" => actual > expected,
                    "GE" => actual >= expected,
                    "LT" => actual < expected,
                    "LE" => actual <= expected,

                    _ => throw new NotSupportedException(
                        $"目前尚未支援 DECIMAL 運算子：{op}")
                };
            }

    private async Task<Dictionary<string, string?>> GetInstanceParametersAsync(
        SqlConnection conn,
        string flowInstanceId)
    {
        var result = new Dictionary<string, string?>(
            StringComparer.OrdinalIgnoreCase);

        const string sql = @"
            SELECT
                PARAM_NAME,
                PARAM_VALUE
            FROM dbo.TB_FLOW_INSTANCE_PARAMETER
            WHERE FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID;
        ";

        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.Add(
            "@FLOW_INSTANCE_ID",
            SqlDbType.VarChar,
            100).Value = flowInstanceId;

        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            result.Add(
                reader.GetString(0),
                reader.IsDBNull(1)
                    ? null
                    : reader.GetString(1));
        }

        return result;
    }
    private async Task<List<FlowOutConditionModel>> GetOutConditionsAsync(
            SqlConnection conn,
            string flowCode,
            int versionNo,
            string stepCode,
            string outCode)
        {
            var result = new List<FlowOutConditionModel>();

            const string sql = @"
                SELECT
                    ID,
                    FLOW_CODE,
                    VERSION_NO,
                    STEP_CODE,
                    OUT_CODE,
                    CONDITION_NO,
                    PARAM_NAME,
                    OPERATOR,
                    COMPARE_VALUE
                FROM dbo.TB_FLOW_OUT_CONDITION
                WHERE FLOW_CODE = @FLOW_CODE
                AND VERSION_NO = @VERSION_NO
                AND STEP_CODE = @STEP_CODE
                AND OUT_CODE = @OUT_CODE
                ORDER BY CONDITION_NO;
            ";

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@FLOW_CODE", SqlDbType.VarChar, 50)
                .Value = flowCode;

            cmd.Parameters.Add("@VERSION_NO", SqlDbType.Int)
                .Value = versionNo;

            cmd.Parameters.Add("@STEP_CODE", SqlDbType.VarChar, 50)
                .Value = stepCode;

            cmd.Parameters.Add("@OUT_CODE", SqlDbType.VarChar, 50)
                .Value = outCode;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new FlowOutConditionModel
                {
                    Id = reader.GetInt64(0),
                    FlowCode = reader.GetString(1),
                    VersionNo = reader.GetInt32(2),
                    StepCode = reader.GetString(3),
                    OutCode = reader.GetString(4),
                    ConditionNo = reader.GetInt32(5),
                    ParamName = reader.GetString(6),
                    Operator = reader.GetString(7),
                    CompareValue = reader.IsDBNull(8)
                        ? null
                        : reader.GetString(8)
                });
            }

            return result;
        }
        private bool EvaluateIntCondition(
            string? actualValue,
            string op,
            string? compareValue)
        {
            // 1. 轉換實際參數值
            if (!int.TryParse(
                actualValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int actual))
            {
                throw new InvalidOperationException(
                    $"無法轉換為 INT：{actualValue}");
            }

            // 2. 轉換條件比較值
            if (!int.TryParse(
                compareValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int expected))
            {
                throw new InvalidOperationException(
                    $"條件比較值無法轉換為 INT：{compareValue}");
            }

            // 3. 執行比較
            return op.ToUpperInvariant() switch
            {
                "EQ" => actual == expected,
                "NE" => actual != expected,
                "GT" => actual > expected,
                "GE" => actual >= expected,
                "LT" => actual < expected,
                "LE" => actual <= expected,

                _ => throw new NotSupportedException(
                    $"目前尚未支援 INT 運算子：{op}")
            };
        }
        private async Task<string> GetParameterDataTypeAsync(
            SqlConnection conn,
            string flowCode,
            int versionNo,
            string paramName)
        {
            const string sql = @"
                SELECT DATA_TYPE
                FROM dbo.TB_FLOW_PARAMETER_DEF
                WHERE FLOW_CODE = @FLOW_CODE
                AND VERSION_NO = @VERSION_NO
                AND PARAM_NAME = @PARAM_NAME;
            ";

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@FLOW_CODE", SqlDbType.VarChar, 50)
                .Value = flowCode;

            cmd.Parameters.Add("@VERSION_NO", SqlDbType.Int)
                .Value = versionNo;

            cmd.Parameters.Add("@PARAM_NAME", SqlDbType.VarChar, 100)
                .Value = paramName;

            var result = await cmd.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    $"找不到參數定義：{flowCode} v{versionNo} / {paramName}");
            }

            return Convert.ToString(result)!.Trim().ToUpperInvariant();
        }
    }
    
}
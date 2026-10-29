using System.Data;
using Microsoft.Data.SqlClient;
using FlowEngineDemo.Models;
using System.Text.Json;
using System.Globalization;
namespace FlowEngineDemo.Services
{
    public class ParameterService : IParameterService
    {
        private readonly string _connectionString;

        public ParameterService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("Elsa")
                ?? throw new InvalidOperationException(
                    "找不到 Elsa 資料庫連線字串。");
        }

        public async Task<List<FlowParameterDefinitionModel>>
            GetParameterDefinitionsAsync(
                string flowCode,
                int versionNo)
        {
            if (string.IsNullOrWhiteSpace(flowCode))
                throw new ArgumentException("FlowCode 不可為空。");

            if (versionNo <= 0)
                throw new ArgumentException("VersionNo 必須大於 0。");

            var result = new List<FlowParameterDefinitionModel>();

            const string sql = @"
                SELECT
                    FLOW_CODE,
                    VERSION_NO,
                    PARAM_NAME,
                    PARAM_DESC,
                    DATA_TYPE,
                    IS_REQUIRED,
                    DEFAULT_VALUE,
                    IS_MUTABLE,
                    CREATED_AT
                FROM dbo.TB_FLOW_PARAMETER_DEF
                WHERE FLOW_CODE = @FLOW_CODE
                  AND VERSION_NO = @VERSION_NO
                ORDER BY PARAM_NAME";

            await using var conn =
                new SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd =
                new SqlCommand(sql, conn);

            cmd.Parameters.Add(
                "@FLOW_CODE",
                SqlDbType.VarChar,
                50).Value = flowCode.Trim();

            cmd.Parameters.Add(
                "@VERSION_NO",
                SqlDbType.Int).Value = versionNo;

            await using var reader =
                await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new FlowParameterDefinitionModel
                {
                    FlowCode = reader.GetString(
                        reader.GetOrdinal("FLOW_CODE")),

                    VersionNo = reader.GetInt32(
                        reader.GetOrdinal("VERSION_NO")),

                    ParamName = reader.GetString(
                        reader.GetOrdinal("PARAM_NAME")),

                    ParamDesc = reader.IsDBNull(
                        reader.GetOrdinal("PARAM_DESC"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("PARAM_DESC")),

                    DataType = reader.GetString(
                        reader.GetOrdinal("DATA_TYPE")),

                    IsRequired = reader.GetBoolean(
                        reader.GetOrdinal("IS_REQUIRED")),

                    DefaultValue = reader.IsDBNull(
                        reader.GetOrdinal("DEFAULT_VALUE"))
                        ? null
                        : reader.GetString(
                            reader.GetOrdinal("DEFAULT_VALUE")),

                    IsMutable = reader.GetBoolean(
                        reader.GetOrdinal("IS_MUTABLE")),

                    CreatedAt =
                        reader.GetFieldValue<DateTimeOffset>(
                            reader.GetOrdinal("CREATED_AT"))
                });
            }

            return result;
        }
        public async Task<ParameterValidationResult>
            ValidateParametersAsync(
                string flowCode,
                int versionNo,
                Dictionary<string, JsonElement> parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);

            var result = new ParameterValidationResult();

            // 取得流程版本的參數定義
            var definitions =
                await GetParameterDefinitionsAsync(
                    flowCode,
                    versionNo);

            // 防止大小寫不同的參數名稱被當成不同參數
            var definitionMap = definitions.ToDictionary(
                x => x.ParamName,
                StringComparer.OrdinalIgnoreCase);

            var inputMap =
                new Dictionary<string, JsonElement>(
                    StringComparer.OrdinalIgnoreCase);

            // 檢查外部傳入的參數
            foreach (var item in parameters)
            {
                if (!inputMap.TryAdd(item.Key, item.Value))
                {
                    result.Errors.Add(
                        $"參數重複：{item.Key}");
                    continue;
                }

                if (!definitionMap.ContainsKey(item.Key))
                {
                    result.Errors.Add(
                        $"未定義的流程參數：{item.Key}");
                }
            }

            // 依照資料庫定義進行驗證
            foreach (var definition in definitions)
            {
                bool hasInput = inputMap.TryGetValue(
                    definition.ParamName,
                    out JsonElement inputValue);

                string? value = null;

                if (hasInput)
                {
                    if (inputValue.ValueKind != JsonValueKind.Null &&
                        inputValue.ValueKind != JsonValueKind.Undefined)
                    {
                        value = inputValue.ValueKind == JsonValueKind.String
                            ? inputValue.GetString()
                            : inputValue.GetRawText();
                    }
                }
                else
                {
                    // 未傳入時使用預設值
                    value = definition.DefaultValue;
                }

                // 必填檢查
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (definition.IsRequired)
                    {
                        result.Errors.Add(
                            $"參數 {definition.ParamName} 為必填。");
                    }

                    result.Parameters[definition.ParamName] = null;
                    continue;
                }

                // 型別檢查與標準化
                if (!TryNormalizeValue(
                        definition.DataType,
                        value,
                        out string? normalizedValue))
                {
                    result.Errors.Add(
                        $"參數 {definition.ParamName} 的資料型別不正確，" +
                        $"預期型別：{definition.DataType}。");

                    continue;
                }

                result.Parameters[definition.ParamName] =
                    normalizedValue;
            }

            result.IsValid = result.Errors.Count == 0;

            return result;
        }
        private static bool TryNormalizeValue(
            string dataType,
            string value,
            out string? normalizedValue)
        {
            normalizedValue = null;

            switch (dataType.ToUpperInvariant())
            {
                case "STRING":

                    normalizedValue = value;
                    return true;

                case "INT":

                    if (int.TryParse(
                            value,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int intValue))
                    {
                        normalizedValue =
                            intValue.ToString(
                                CultureInfo.InvariantCulture);

                        return true;
                    }

                    return false;

                case "DECIMAL":

                    if (decimal.TryParse(
                            value,
                            NumberStyles.AllowLeadingSign |
                            NumberStyles.AllowDecimalPoint,
                            CultureInfo.InvariantCulture,
                            out decimal decimalValue))
                    {
                        normalizedValue =
                            decimalValue.ToString(
                                CultureInfo.InvariantCulture);

                        return true;
                    }

                    return false;

                case "BOOL":

                    if (bool.TryParse(value, out bool boolValue))
                    {
                        normalizedValue =
                            boolValue ? "true" : "false";

                        return true;
                    }

                    return false;

                case "DATE":

                    if (DateTime.TryParseExact(
                            value,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime dateValue))
                    {
                        normalizedValue =
                            dateValue.ToString(
                                "yyyy-MM-dd",
                                CultureInfo.InvariantCulture);

                        return true;
                    }

                    return false;

                case "DATETIME":

                    if (DateTimeOffset.TryParse(
                            value,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTimeOffset dateTimeValue))
                    {
                        normalizedValue =
                            dateTimeValue.ToUniversalTime().ToString(
                                "O",
                                CultureInfo.InvariantCulture);

                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }
        public async Task SaveInstanceParametersAsync(
            string flowInstanceId,
            Dictionary<string, JsonElement> parameters)
        {
            if (string.IsNullOrWhiteSpace(flowInstanceId))
                throw new ArgumentException("FlowInstanceId 不可為空。");

            ArgumentNullException.ThrowIfNull(parameters);

            flowInstanceId = flowInstanceId.Trim();

            await using var conn = new SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // 1. 取得流程實例的流程代碼及版本
                const string sqlInstance = @"
                    SELECT FLOW_CODE, VERSION_NO
                    FROM dbo.TB_FLOW_INSTANCE WITH (UPDLOCK, HOLDLOCK)
                    WHERE FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID;
                ";

                string flowCode;
                int versionNo;

                await using (var cmd = new SqlCommand(
                    sqlInstance, conn, transaction))
                {
                    cmd.Parameters.Add(
                        "@FLOW_INSTANCE_ID",
                        SqlDbType.VarChar,
                        100).Value = flowInstanceId;

                    await using var reader =
                        await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        throw new InvalidOperationException(
                            $"流程實例不存在：{flowInstanceId}");
                    }

                    flowCode = reader.GetString(0);
                    versionNo = reader.GetInt32(1);
                }

                // 2. 驗證參數
                var validation =
                    await ValidateParametersAsync(
                        flowCode,
                        versionNo,
                        parameters);

                if (!validation.IsValid)
                {
                    throw new InvalidOperationException(
                        string.Join("; ", validation.Errors));
                }

                // 3. 檢查是否已儲存過參數
                const string sqlCount = @"
                    SELECT COUNT(1)
                    FROM dbo.TB_FLOW_INSTANCE_PARAMETER
                    WHERE FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID;
                ";

                await using (var cmd = new SqlCommand(
                    sqlCount, conn, transaction))
                {
                    cmd.Parameters.Add(
                        "@FLOW_INSTANCE_ID",
                        SqlDbType.VarChar,
                        100).Value = flowInstanceId;

                    int count = Convert.ToInt32(
                        await cmd.ExecuteScalarAsync());

                    if (count > 0)
                    {
                        throw new InvalidOperationException(
                            "此流程實例已儲存參數，不允許重複初始化。");
                    }
                }

                // 4. 寫入參數
                const string sqlInsert = @"
                    INSERT INTO dbo.TB_FLOW_INSTANCE_PARAMETER
                    (
                        FLOW_INSTANCE_ID,
                        PARAM_NAME,
                        PARAM_VALUE
                    )
                    VALUES
                    (
                        @FLOW_INSTANCE_ID,
                        @PARAM_NAME,
                        @PARAM_VALUE
                    );
                ";

                foreach (var item in validation.Parameters)
                {
                    await using var cmd = new SqlCommand(
                        sqlInsert, conn, transaction);

                    cmd.Parameters.Add(
                        "@FLOW_INSTANCE_ID",
                        SqlDbType.VarChar,
                        100).Value = flowInstanceId;

                    cmd.Parameters.Add(
                        "@PARAM_NAME",
                        SqlDbType.VarChar,
                        100).Value = item.Key;

                    cmd.Parameters.Add(
                        "@PARAM_VALUE",
                        SqlDbType.NVarChar,
                        -1).Value =
                            (object?)item.Value ?? DBNull.Value;

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Dictionary<string, string?>> GetInstanceParametersAsync(
            string flowInstanceId)
        {
            if (string.IsNullOrWhiteSpace(flowInstanceId))
            {
                throw new ArgumentException(
                    "FlowInstanceId 不可為空。",
                    nameof(flowInstanceId));
            }

            var result = new Dictionary<string, string?>(
                StringComparer.OrdinalIgnoreCase);

            const string sql = @"
                SELECT
                    PARAM_NAME,
                    PARAM_VALUE
                FROM dbo.TB_FLOW_INSTANCE_PARAMETER
                WHERE FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID
                ORDER BY PARAM_NAME;
            ";

            await using var conn = new SqlConnection(_connectionString);

            await conn.OpenAsync();

            await using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add(
                "@FLOW_INSTANCE_ID",
                SqlDbType.VarChar,
                100).Value = flowInstanceId.Trim();

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string paramName = reader.GetString(0);

                string? paramValue = reader.IsDBNull(1)
                    ? null
                    : reader.GetString(1);

                result[paramName] = paramValue;
            }

            return result;
        }
        public async Task UpdateInstanceParametersAsync(
            string flowInstanceId,
            Dictionary<string, JsonElement> parameters)
        {
            if (string.IsNullOrWhiteSpace(flowInstanceId))
                throw new ArgumentException("FlowInstanceId 不可為空。");

            ArgumentNullException.ThrowIfNull(parameters);

            if (parameters.Count == 0)
                throw new InvalidOperationException("沒有提供需要更新的參數。");

            flowInstanceId = flowInstanceId.Trim();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var transaction =
                (SqlTransaction)await conn.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            try
            {
                // 1. 取得流程實例資訊
                const string sqlInstance = @"
                    SELECT FLOW_CODE, VERSION_NO
                    FROM dbo.TB_FLOW_INSTANCE WITH (UPDLOCK, HOLDLOCK)
                    WHERE FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID;
                ";

                string flowCode;
                int versionNo;

                await using (var cmd = new SqlCommand(
                    sqlInstance, conn, transaction))
                {
                    cmd.Parameters.Add(
                        "@FLOW_INSTANCE_ID",
                        SqlDbType.VarChar,
                        100).Value = flowInstanceId;

                    await using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                        throw new InvalidOperationException(
                            $"流程實例不存在：{flowInstanceId}");

                    flowCode = reader.GetString(0);
                    versionNo = reader.GetInt32(1);
                }

                // 2. 取得參數定義
                var definitions =
                    await GetParameterDefinitionsAsync(flowCode, versionNo);

                var definitionMap = definitions.ToDictionary(
                    x => x.ParamName,
                    StringComparer.OrdinalIgnoreCase);

                // 3. 檢查參數是否允許修改
                foreach (var item in parameters)
                {
                    if (!definitionMap.TryGetValue(
                        item.Key, out var definition))
                    {
                        throw new InvalidOperationException(
                            $"未定義的流程參數：{item.Key}");
                    }

                    if (!definition.IsMutable)
                    {
                        throw new InvalidOperationException(
                            $"參數 {item.Key} 不允許修改（IS_MUTABLE = 0）。");
                    }
                }

                // 4. 驗證更新後的完整參數集合
                var currentParameters =
                    await GetInstanceParametersAsync(flowInstanceId);

                if (currentParameters.Count == 0)
                    throw new InvalidOperationException(
                        "此流程實例尚未初始化參數。");

                var mergedParameters =
                    new Dictionary<string, JsonElement>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var item in currentParameters)
                {
                    mergedParameters[item.Key] =
                        JsonSerializer.SerializeToElement(item.Value);
                }

                foreach (var item in parameters)
                {
                    if (!currentParameters.ContainsKey(item.Key))
                        throw new InvalidOperationException(
                            $"參數 {item.Key} 尚未初始化，不允許更新。");

                    mergedParameters[item.Key] = item.Value;
                }

                var validation =
                    await ValidateParametersAsync(
                        flowCode,
                        versionNo,
                        mergedParameters);

                if (!validation.IsValid)
                    throw new InvalidOperationException(
                        string.Join("; ", validation.Errors));

                // 5. 更新指定參數
                const string sqlUpdate = @"
                    UPDATE dbo.TB_FLOW_INSTANCE_PARAMETER
                    SET
                        PARAM_VALUE = @PARAM_VALUE,
                        UPDATED_AT = SYSDATETIMEOFFSET()
                    WHERE FLOW_INSTANCE_ID = @FLOW_INSTANCE_ID
                    AND PARAM_NAME = @PARAM_NAME;
                ";

                foreach (var item in parameters)
                {
                    await using var cmd = new SqlCommand(
                        sqlUpdate, conn, transaction);

                    cmd.Parameters.Add(
                        "@FLOW_INSTANCE_ID",
                        SqlDbType.VarChar,
                        100).Value = flowInstanceId;

                    cmd.Parameters.Add(
                        "@PARAM_NAME",
                        SqlDbType.VarChar,
                        100).Value = item.Key;

                    cmd.Parameters.Add(
                        "@PARAM_VALUE",
                        SqlDbType.NVarChar,
                        -1).Value =
                            (object?)validation.Parameters[item.Key]
                            ?? DBNull.Value;

                    int affectedRows = await cmd.ExecuteNonQueryAsync();

                    if (affectedRows != 1)
                        throw new InvalidOperationException(
                            $"參數 {item.Key} 更新失敗，資料可能不存在。");
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
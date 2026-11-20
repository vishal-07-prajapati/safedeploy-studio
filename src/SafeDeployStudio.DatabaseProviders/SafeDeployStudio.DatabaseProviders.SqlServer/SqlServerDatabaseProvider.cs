using System.Data;
using System.Diagnostics;
using Dapper;
using Microsoft.Data.SqlClient;
using SafeDeployStudio.DatabaseProviders.Abstractions;
using SafeDeployStudio.DatabaseProviders.Abstractions.Models;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.DatabaseProviders.SqlServer;

public class SqlServerDatabaseProvider : IDatabaseProvider
{
    private readonly IScriptBatchSplitter _batchSplitter;

    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;

    public SqlServerDatabaseProvider(IScriptBatchSplitter? batchSplitter = null)
    {
        _batchSplitter = batchSplitter ?? new SqlServerBatchSplitter();
    }

    public async Task<bool> TestConnectionAsync(DatabaseConnection connection, CancellationToken ct = default)
    {
        try
        {
            var connStr = SqlServerConnectionStringBuilder.Build(connection);
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync(ct);
            var result = await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: ct));
            return result == 1;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<DatabaseSummary>> ListDatabasesAsync(DatabaseConnection connection, CancellationToken ct = default)
    {
        var connStr = SqlServerConnectionStringBuilder.Build(connection, overrideDatabase: "master");
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT 
                name AS Name,
                state_desc AS State,
                collation_name AS Collation
            FROM sys.databases
            WHERE database_id > 4 AND state_desc = 'ONLINE'
            ORDER BY name;";

        var databases = await conn.QueryAsync<DatabaseSummary>(new CommandDefinition(sql, cancellationToken: ct));
        return databases.ToList();
    }

    public async Task<IReadOnlyList<SchemaObjectInfo>> ListObjectsAsync(
        DatabaseConnection connection,
        DatabaseObjectType? type = null,
        CancellationToken ct = default)
    {
        var connStr = SqlServerConnectionStringBuilder.Build(connection);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        string typeFilter = type switch
        {
            DatabaseObjectType.StoredProcedure => "AND o.type = 'P'",
            DatabaseObjectType.View => "AND o.type = 'V'",
            DatabaseObjectType.Function => "AND o.type IN ('FN', 'IF', 'TF')",
            DatabaseObjectType.Table => "AND o.type = 'U'",
            DatabaseObjectType.Trigger => "AND o.type = 'TR'",
            _ => "AND o.type IN ('P', 'V', 'FN', 'IF', 'TF', 'U', 'TR')"
        };

        var sql = $@"
            SELECT 
                s.name AS SchemaName,
                o.name AS ObjectName,
                CASE o.type
                    WHEN 'P' THEN 1
                    WHEN 'V' THEN 2
                    WHEN 'FN' THEN 3
                    WHEN 'IF' THEN 3
                    WHEN 'TF' THEN 3
                    WHEN 'U' THEN 4
                    WHEN 'TR' THEN 5
                    ELSE 1
                END AS ObjectType,
                o.modify_date AS LastModifiedUtc
            FROM sys.objects o
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE o.is_ms_shipped = 0 {typeFilter}
            ORDER BY s.name, o.name;";

        var items = await conn.QueryAsync<SchemaObjectInfo>(new CommandDefinition(sql, cancellationToken: ct));
        return items.ToList();
    }

    public async Task<string> GetObjectDefinitionAsync(
        DatabaseConnection connection,
        string schemaName,
        string objectName,
        DatabaseObjectType type,
        CancellationToken ct = default)
    {
        var connStr = SqlServerConnectionStringBuilder.Build(connection);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync(ct);

        const string sql = @"
            SELECT m.definition 
            FROM sys.sql_modules m 
            INNER JOIN sys.objects o ON m.object_id = o.object_id 
            INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
            WHERE s.name = @Schema AND o.name = @Name;";

        var definition = await conn.ExecuteScalarAsync<string?>(
            new CommandDefinition(sql, new { Schema = schemaName, Name = objectName }, cancellationToken: ct));

        return definition ?? string.Empty;
    }

    public async Task<DryRunExecutionResult> ExecuteDryRunAsync(
        DatabaseConnection connection,
        string script,
        int timeoutSeconds = 60,
        CancellationToken ct = default)
    {
        var result = new DryRunExecutionResult();
        var sw = Stopwatch.StartNew();

        var batches = _batchSplitter.SplitBatches(script);
        if (batches.Count == 0)
        {
            result.IsSuccessful = true;
            result.Messages.Add("Empty script provided. Nothing to execute.");
            return result;
        }

        var connStr = SqlServerConnectionStringBuilder.Build(connection);
        await using var conn = new SqlConnection(connStr);

        conn.InfoMessage += (s, e) =>
        {
            result.Messages.Add($"[INFO] {e.Message}");
        };

        try
        {
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            int totalRows = 0;
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                await using var cmd = new SqlCommand(batch, conn, (SqlTransaction)tx)
                {
                    CommandTimeout = timeoutSeconds
                };
                var rows = await cmd.ExecuteNonQueryAsync(ct);
                if (rows > 0) totalRows += rows;
                result.Messages.Add($"Batch {i + 1}/{batches.Count} executed successfully in sandbox transaction.");
            }

            // Always rollback in dry run!
            await tx.RollbackAsync(ct);
            result.RolledBackCleanly = true;
            result.IsSuccessful = true;
            result.RowsAffected = totalRows;
            result.Messages.Add("Dry-run sandbox executed all statements and verified clean rollback.");
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.Messages.Add($"[ERROR] Dry-run failed: {ex.Message}");
            result.RolledBackCleanly = true; // Connection closure automatically rolls back uncommitted transaction
        }
        finally
        {
            sw.Stop();
            result.ExecutionDurationMs = sw.ElapsedMilliseconds;
        }

        return result;
    }

    public async Task<BatchExecutionResult> ExecuteMigrationScriptAsync(
        DatabaseConnection connection,
        string script,
        Action<string>? logCallback = null,
        int timeoutSeconds = 300,
        CancellationToken ct = default)
    {
        var result = new BatchExecutionResult();
        var sw = Stopwatch.StartNew();

        var batches = _batchSplitter.SplitBatches(script);
        if (batches.Count == 0)
        {
            result.IsSuccessful = true;
            return result;
        }

        var connStr = SqlServerConnectionStringBuilder.Build(connection);
        await using var conn = new SqlConnection(connStr);

        conn.InfoMessage += (s, e) =>
        {
            var msg = $"[INFO] {e.Message}";
            result.Messages.Add(msg);
            logCallback?.Invoke(msg);
        };

        try
        {
            await conn.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var startMsg = $"Executing batch {i + 1}/{batches.Count}...";
                result.Messages.Add(startMsg);
                logCallback?.Invoke(startMsg);

                await using var cmd = new SqlCommand(batch, conn, (SqlTransaction)tx)
                {
                    CommandTimeout = timeoutSeconds
                };

                await cmd.ExecuteNonQueryAsync(ct);
                result.BatchesExecuted++;
            }

            await tx.CommitAsync(ct);
            result.IsSuccessful = true;
            var successMsg = $"Successfully applied all {batches.Count} batches.";
            result.Messages.Add(successMsg);
            logCallback?.Invoke(successMsg);
        }
        catch (Exception ex)
        {
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;
            result.FailedAtBatchIndex = result.BatchesExecuted;
            var errMsg = $"[FAILED] Batch {result.BatchesExecuted + 1} execution failed: {ex.Message}. Transaction rolled back.";
            result.Messages.Add(errMsg);
            logCallback?.Invoke(errMsg);
        }
        finally
        {
            sw.Stop();
            result.TotalDurationMs = sw.ElapsedMilliseconds;
        }

        return result;
    }

    public string GenerateDropIfExistsScript(string schemaName, string objectName, DatabaseObjectType type)
    {
        var safeSchema = schemaName.Replace("]", "]]");
        var safeObject = objectName.Replace("]", "]]");

        return type switch
        {
            DatabaseObjectType.StoredProcedure =>
                $"IF OBJECT_ID(N'[{safeSchema}].[{safeObject}]', 'P') IS NOT NULL\n    DROP PROCEDURE [{safeSchema}].[{safeObject}];\nGO\n",
            DatabaseObjectType.View =>
                $"IF OBJECT_ID(N'[{safeSchema}].[{safeObject}]', 'V') IS NOT NULL\n    DROP VIEW [{safeSchema}].[{safeObject}];\nGO\n",
            DatabaseObjectType.Function =>
                $"IF OBJECT_ID(N'[{safeSchema}].[{safeObject}]', 'FN') IS NOT NULL OR OBJECT_ID(N'[{safeSchema}].[{safeObject}]', 'IF') IS NOT NULL\n    DROP FUNCTION [{safeSchema}].[{safeObject}];\nGO\n",
            DatabaseObjectType.Trigger =>
                $"IF OBJECT_ID(N'[{safeSchema}].[{safeObject}]', 'TR') IS NOT NULL\n    DROP TRIGGER [{safeSchema}].[{safeObject}];\nGO\n",
            DatabaseObjectType.Table =>
                $"IF OBJECT_ID(N'[{safeSchema}].[{safeObject}]', 'U') IS NOT NULL\n    DROP TABLE [{safeSchema}].[{safeObject}];\nGO\n",
            _ => $"-- Drop script not generated for unsupported object type\n"
        };
    }
}

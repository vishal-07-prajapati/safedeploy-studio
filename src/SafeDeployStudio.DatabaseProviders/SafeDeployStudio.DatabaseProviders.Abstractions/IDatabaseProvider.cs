using SafeDeployStudio.DatabaseProviders.Abstractions.Models;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.DatabaseProviders.Abstractions;

public interface IDatabaseProvider
{
    DatabaseProviderType ProviderType { get; }

    Task<bool> TestConnectionAsync(DatabaseConnection connection, CancellationToken ct = default);

    Task<IReadOnlyList<DatabaseSummary>> ListDatabasesAsync(DatabaseConnection connection, CancellationToken ct = default);

    Task<IReadOnlyList<SchemaObjectInfo>> ListObjectsAsync(
        DatabaseConnection connection,
        DatabaseObjectType? type = null,
        CancellationToken ct = default);

    Task<string> GetObjectDefinitionAsync(
        DatabaseConnection connection,
        string schemaName,
        string objectName,
        DatabaseObjectType type,
        CancellationToken ct = default);

    Task<DryRunExecutionResult> ExecuteDryRunAsync(
        DatabaseConnection connection,
        string script,
        int timeoutSeconds = 60,
        CancellationToken ct = default);

    Task<BatchExecutionResult> ExecuteMigrationScriptAsync(
        DatabaseConnection connection,
        string script,
        Action<string>? logCallback = null,
        int timeoutSeconds = 300,
        CancellationToken ct = default);

    string GenerateDropIfExistsScript(string schemaName, string objectName, DatabaseObjectType type);
}

public interface IScriptBatchSplitter
{
    IReadOnlyList<string> SplitBatches(string fullScript);
}

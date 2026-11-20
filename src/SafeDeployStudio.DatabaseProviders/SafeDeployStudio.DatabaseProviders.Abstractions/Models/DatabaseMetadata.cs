using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.DatabaseProviders.Abstractions.Models;

public record SchemaObjectInfo(
    string SchemaName,
    string ObjectName,
    DatabaseObjectType ObjectType,
    DateTime? LastModifiedUtc = null,
    string? DefinitionHash = null
);

public record DatabaseSummary(
    string Name,
    string State,
    long SizeMb = 0,
    string? Collation = null
);

public class BatchExecutionResult
{
    public bool IsSuccessful { get; set; }
    public int BatchesExecuted { get; set; }
    public long TotalDurationMs { get; set; }
    public List<string> Messages { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public int? FailedAtBatchIndex { get; set; }
}

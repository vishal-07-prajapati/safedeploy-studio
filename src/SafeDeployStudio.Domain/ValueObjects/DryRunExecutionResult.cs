namespace SafeDeployStudio.Domain.ValueObjects;

public class DryRunExecutionResult
{
    public bool IsSuccessful { get; set; }
    public long ExecutionDurationMs { get; set; }
    public int RowsAffected { get; set; }
    public List<string> Messages { get; set; } = [];
    public string? ErrorMessage { get; set; }
    public bool RolledBackCleanly { get; set; }
}

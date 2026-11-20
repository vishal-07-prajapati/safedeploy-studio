namespace SafeDeployStudio.Domain.ValueObjects;

public enum DiffLineType
{
    Unchanged = 0,
    Inserted = 1,
    Deleted = 2,
    Modified = 3
}

public record DiffLine(
    int? OldLineNumber,
    int? NewLineNumber,
    string Text,
    DiffLineType Type
);

public class DiffHunk
{
    public int OldStartLine { get; set; }
    public int OldLineCount { get; set; }
    public int NewStartLine { get; set; }
    public int NewLineCount { get; set; }
    public List<DiffLine> Lines { get; set; } = [];
}

public class SqlDiffResult
{
    public string ObjectName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
    public bool HasDifferences => AdditionsCount > 0 || DeletionsCount > 0 || ModificationsCount > 0;
    public int AdditionsCount { get; set; }
    public int DeletionsCount { get; set; }
    public int ModificationsCount { get; set; }
    public List<DiffHunk> Hunks { get; set; } = [];
    public List<DiffLine> AllLines { get; set; } = [];
}

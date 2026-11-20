using System.Text.RegularExpressions;
using SafeDeployStudio.AI.Models;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.AI.Services;

public interface ISargabilityAnalyzer
{
    IReadOnlyList<SargabilityWarning> Analyze(string sql);
}

public partial class SargabilityAnalyzer : ISargabilityAnalyzer
{
    public IReadOnlyList<SargabilityWarning> Analyze(string sql)
    {
        var warnings = new List<SargabilityWarning>();
        if (string.IsNullOrWhiteSpace(sql))
            return warnings;

        var lines = sql.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        bool hasNoCount = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            if (trimmed.StartsWith("--") || trimmed.StartsWith("/*"))
                continue;

            if (trimmed.Contains("SET NOCOUNT ON", StringComparison.OrdinalIgnoreCase))
            {
                hasNoCount = true;
            }

            // 1. Function on column in WHERE/JOIN (YEAR, MONTH, DATEPART)
            var dateFuncMatch = DateFuncRegex().Match(line);
            if (dateFuncMatch.Success)
            {
                warnings.Add(new SargabilityWarning(
                    "SARG001",
                    "Non-SARGable Date Function",
                    $"Wrapping a column in '{dateFuncMatch.Value}' prevents index seek operations and forces a full table/index scan.",
                    i + 1,
                    dateFuncMatch.Value,
                    "Rewrite using explicit date boundaries: e.g. 'Column >= @Start AND Column < @End'.",
                    RiskLevel.High
                ));
            }

            // 2. ISNULL / COALESCE on column in predicate
            var isNullMatch = IsNullPredRegex().Match(line);
            if (isNullMatch.Success)
            {
                warnings.Add(new SargabilityWarning(
                    "SARG002",
                    "Non-SARGable ISNULL/COALESCE",
                    $"Expression '{isNullMatch.Value}' invalidates index seek capability.",
                    i + 1,
                    isNullMatch.Value,
                    "Rewrite as '(Column = @Val OR (@Val IS NULL AND Column IS NULL))' or use query branching.",
                    RiskLevel.High
                ));
            }

            // 3. Leading Wildcard in LIKE
            var likeMatch = LeadingWildcardRegex().Match(line);
            if (likeMatch.Success)
            {
                warnings.Add(new SargabilityWarning(
                    "SARG003",
                    "Leading Wildcard Pattern",
                    $"Leading wildcard '{likeMatch.Value}' causes a full index scan.",
                    i + 1,
                    likeMatch.Value,
                    "Consider full-text search, trie-based search, or remove leading '%' if exact prefix search is sufficient.",
                    RiskLevel.Medium
                ));
            }

            // 4. SELECT * Anti-Pattern
            var selectStarMatch = SelectStarRegex().Match(line);
            if (selectStarMatch.Success)
            {
                warnings.Add(new SargabilityWarning(
                    "PERF001",
                    "SELECT * Prohibited in Stored Procedures",
                    "Using 'SELECT *' causes unnecessary network overhead, prevents covering index usage, and risks breaking client contracts when columns change.",
                    i + 1,
                    selectStarMatch.Value,
                    "Specify only the required column names explicitly in the SELECT list.",
                    RiskLevel.Medium
                ));
            }

            // 5. Cursor declaration
            var cursorMatch = CursorRegex().Match(line);
            if (cursorMatch.Success)
            {
                warnings.Add(new SargabilityWarning(
                    "PERF002",
                    "T-SQL Cursor Detected",
                    "Cursors process rows iteratively and consume significant server memory and lock resources.",
                    i + 1,
                    cursorMatch.Value,
                    "Replace cursor with set-based operations (e.g. window functions, CROSS APPLY, CTEs, or temporary staging tables).",
                    RiskLevel.Medium
                ));
            }
        }

        // 6. Check for missing SET NOCOUNT ON in stored procedures
        if (sql.Contains("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase))
        {
            if (!hasNoCount)
            {
                warnings.Add(new SargabilityWarning(
                    "PERF003",
                    "Missing 'SET NOCOUNT ON'",
                    "Stored procedure does not specify 'SET NOCOUNT ON;'. This causes unnecessary DONE_IN_PROC network roundtrips for each statement.",
                    1,
                    "PROCEDURE",
                    "Add 'SET NOCOUNT ON;' immediately after the BEGIN block of the procedure.",
                    RiskLevel.Low
                ));
            }
        }

        return warnings;
    }

    [GeneratedRegex(@"\b(YEAR|MONTH|DAY|DATEPART)\s*\(\s*\[?[a-zA-Z0-9_]+\]?\s*\)\s*(=|<|>|<=|>=|BETWEEN)", RegexOptions.IgnoreCase)]
    private static partial Regex DateFuncRegex();

    [GeneratedRegex(@"\b(ISNULL|COALESCE)\s*\(\s*\[?[a-zA-Z0-9_]+\]?\s*,[^)]+\)\s*(=|<|>|<=|>=|LIKE)", RegexOptions.IgnoreCase)]
    private static partial Regex IsNullPredRegex();

    [GeneratedRegex(@"\bLIKE\s+'%[^']+'", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingWildcardRegex();

    [GeneratedRegex(@"\bSELECT\s+\*\s+FROM\b", RegexOptions.IgnoreCase)]
    private static partial Regex SelectStarRegex();

    [GeneratedRegex(@"\bDECLARE\s+[a-zA-Z0-9_@]+\s+CURSOR\b", RegexOptions.IgnoreCase)]
    private static partial Regex CursorRegex();
}

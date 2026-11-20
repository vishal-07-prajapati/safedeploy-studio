using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Deployment;

public interface ISqlDiffEngine
{
    SqlDiffResult CompareSql(
        string oldSql,
        string newSql,
        string objectName = "",
        string schemaName = "dbo",
        bool ignoreWhitespace = false);
}

public class SqlDiffEngine : ISqlDiffEngine
{
    private readonly ISideBySideDiffBuilder _diffBuilder;

    public SqlDiffEngine()
    {
        var differ = new Differ();
        _diffBuilder = new SideBySideDiffBuilder(differ);
    }

    public SqlDiffResult CompareSql(
        string oldSql,
        string newSql,
        string objectName = "",
        string schemaName = "dbo",
        bool ignoreWhitespace = false)
    {
        var normalizedOld = Normalize(oldSql);
        var normalizedNew = Normalize(newSql);

        var diffModel = _diffBuilder.BuildDiffModel(
            normalizedOld,
            normalizedNew);

        var result = new SqlDiffResult
        {
            ObjectName = objectName,
            SchemaName = schemaName,
            OldText = normalizedOld,
            NewText = normalizedNew
        };

        var allLines = new List<DiffLine>();
        int additions = 0;
        int deletions = 0;
        int modifications = 0;

        var oldLines = diffModel.OldText.Lines;
        var newLines = diffModel.NewText.Lines;
        var maxCount = Math.Max(oldLines.Count, newLines.Count);

        for (int i = 0; i < maxCount; i++)
        {
            var oldLine = i < oldLines.Count ? oldLines[i] : null;
            var newLine = i < newLines.Count ? newLines[i] : null;

            if (oldLine != null && newLine != null)
            {
                if (oldLine.Type == ChangeType.Unchanged && newLine.Type == ChangeType.Unchanged)
                {
                    allLines.Add(new DiffLine(
                        oldLine.Position,
                        newLine.Position,
                        newLine.Text ?? string.Empty,
                        DiffLineType.Unchanged));
                }
                else if (oldLine.Type == ChangeType.Deleted)
                {
                    deletions++;
                    allLines.Add(new DiffLine(
                        oldLine.Position,
                        null,
                        oldLine.Text ?? string.Empty,
                        DiffLineType.Deleted));
                }
                else if (newLine.Type == ChangeType.Inserted)
                {
                    additions++;
                    allLines.Add(new DiffLine(
                        null,
                        newLine.Position,
                        newLine.Text ?? string.Empty,
                        DiffLineType.Inserted));
                }
                else if (oldLine.Type == ChangeType.Modified || newLine.Type == ChangeType.Modified)
                {
                    modifications++;
                    allLines.Add(new DiffLine(
                        oldLine.Position,
                        newLine.Position,
                        newLine.Text ?? string.Empty,
                        DiffLineType.Modified));
                }
            }
            else if (oldLine != null)
            {
                deletions++;
                allLines.Add(new DiffLine(
                    oldLine.Position,
                    null,
                    oldLine.Text ?? string.Empty,
                    DiffLineType.Deleted));
            }
            else if (newLine != null)
            {
                additions++;
                allLines.Add(new DiffLine(
                    null,
                    newLine.Position,
                    newLine.Text ?? string.Empty,
                    DiffLineType.Inserted));
            }
        }

        result.AdditionsCount = additions;
        result.DeletionsCount = deletions;
        result.ModificationsCount = modifications;
        result.AllLines = allLines;

        return result;
    }

    private static string Normalize(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return string.Empty;
        // Normalize CRLF to LF then back to standard Environment.NewLine for consistent comparison
        return sql.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }
}

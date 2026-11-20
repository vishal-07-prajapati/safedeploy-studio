using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SafeDeployStudio.DatabaseProviders.Abstractions;

namespace SafeDeployStudio.DatabaseProviders.SqlServer;

public partial class SqlServerBatchSplitter : IScriptBatchSplitter
{
    public IReadOnlyList<string> SplitBatches(string fullScript)
    {
        if (string.IsNullOrWhiteSpace(fullScript))
            return [];

        try
        {
            var parser = new TSql160Parser(initialQuotedIdentifiers: true);
            using var reader = new StringReader(fullScript);
            var fragment = parser.Parse(reader, out IList<ParseError> errors);

            if (errors.Count == 0 && fragment is TSqlScript script && script.Batches.Count > 0)
            {
                var batches = new List<string>();
                foreach (var batch in script.Batches)
                {
                    if (batch.FragmentLength <= 0) continue;
                    var batchText = fullScript.Substring(batch.StartOffset, batch.FragmentLength).Trim();
                    if (!string.IsNullOrWhiteSpace(batchText))
                    {
                        batches.Add(batchText);
                    }
                }
                if (batches.Count > 0)
                {
                    return batches;
                }
            }
        }
        catch
        {
            // Fall back to token-aware regex parser below
        }

        return FallbackRegexSplit(fullScript);
    }

    private static List<string> FallbackRegexSplit(string script)
    {
        var batches = new List<string>();
        // Match GO on its own line, ignoring comments
        var regex = GoBatchRegex();
        var parts = regex.Split(script);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                batches.Add(trimmed);
            }
        }

        return batches.Count > 0 ? batches : [script.Trim()];
    }

    [GeneratedRegex(@"^\s*GO\s*(?:--.*)?$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex GoBatchRegex();
}

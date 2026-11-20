using System.Text.Json;
using System.Text.RegularExpressions;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails.Rules;

public partial class ForbiddenAdminKeywordsRule : IGuardrailRule
{
    public string Name => "Forbidden Admin Keywords";

    public Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var violations = new List<GuardrailViolation>();
        if (string.IsNullOrWhiteSpace(sql))
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xp_cmdshell", "sp_configure", "SHUTDOWN", "DROP DATABASE", "RECONFIGURE"
        };

        if (!string.IsNullOrWhiteSpace(policy.ForbiddenKeywordsJson))
        {
            try
            {
                var custom = JsonSerializer.Deserialize<List<string>>(policy.ForbiddenKeywordsJson);
                if (custom != null)
                {
                    foreach (var k in custom)
                    {
                        if (!string.IsNullOrWhiteSpace(k))
                            keywords.Add(k.Trim());
                    }
                }
            }
            catch
            {
                // Ignore parse error, use defaults
            }
        }

        var lines = sql.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            // Skip commented lines
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("--") || trimmed.StartsWith("/*"))
                continue;

            foreach (var keyword in keywords)
            {
                var pattern = $@"\b{Regex.Escape(keyword)}\b";
                var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    violations.Add(new GuardrailViolation(
                        "ForbiddenAdminKeywords",
                        GuardrailSeverity.Critical,
                        $"Unauthorized administrative command '{keyword}' detected.",
                        i + 1,
                        match.Value,
                        $"Administrative command '{keyword}' is forbidden in application migrations."
                    ));
                }
            }
        }

        return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
    }
}

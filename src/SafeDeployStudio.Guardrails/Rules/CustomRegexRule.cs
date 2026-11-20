using System.Text.Json;
using System.Text.RegularExpressions;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails.Rules;

public class CustomRegexRuleDefinition
{
    public string Pattern { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public GuardrailSeverity Severity { get; set; } = GuardrailSeverity.Warning;
    public string? Remediation { get; set; }
}

public class CustomRegexRule : IGuardrailRule
{
    public string Name => "Custom Regex Rules";

    public Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var violations = new List<GuardrailViolation>();
        if (string.IsNullOrWhiteSpace(policy.CustomRegexRulesJson) || string.IsNullOrWhiteSpace(sql))
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        List<CustomRegexRuleDefinition>? rules = null;
        try
        {
            rules = JsonSerializer.Deserialize<List<CustomRegexRuleDefinition>>(policy.CustomRegexRulesJson);
        }
        catch
        {
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
        }

        if (rules == null || rules.Count == 0)
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        var lines = sql.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Pattern)) continue;

            try
            {
                var regex = new Regex(rule.Pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        violations.Add(new GuardrailViolation(
                            "CustomRegexRule",
                            rule.Severity,
                            string.IsNullOrWhiteSpace(rule.Message) ? $"Violation matching pattern '{rule.Pattern}'." : rule.Message,
                            i + 1,
                            match.Value,
                            rule.Remediation
                        ));
                    }
                }
            }
            catch
            {
                // Ignore invalid regex
            }
        }

        return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
    }
}

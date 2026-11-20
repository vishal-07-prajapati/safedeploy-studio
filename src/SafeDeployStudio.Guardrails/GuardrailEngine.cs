using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;
using SafeDeployStudio.Guardrails.Rules;

namespace SafeDeployStudio.Guardrails;

public class GuardrailEngine : IGuardrailEngine
{
    private readonly IEnumerable<IGuardrailRule> _rules;

    public GuardrailEngine(IEnumerable<IGuardrailRule>? rules = null)
    {
        _rules = rules ?? new IGuardrailRule[]
        {
            new BlockPhysicalTableDdlRule(),
            new ForbiddenAdminKeywordsRule(),
            new AsciiComplianceRule(),
            new UnqualifiedUpdateDeleteRule(),
            new ScriptSizeLimitRule(),
            new CustomRegexRule()
        };
    }

    public async Task<GuardrailResult> ValidateScriptAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var result = new GuardrailResult();
        if (string.IsNullOrWhiteSpace(sql))
            return result;

        foreach (var rule in _rules)
        {
            ct.ThrowIfCancellationRequested();
            var violations = await rule.EvaluateAsync(sql, policy, ct);
            result.Violations.AddRange(violations);
        }

        // Compute overall estimated risk
        if (result.Violations.Any(v => v.Severity == GuardrailSeverity.Critical))
        {
            result.EstimatedRisk = RiskLevel.Critical;
        }
        else if (result.Violations.Any(v => v.Severity == GuardrailSeverity.Error))
        {
            result.EstimatedRisk = RiskLevel.High;
        }
        else if (result.Violations.Any(v => v.Severity == GuardrailSeverity.Warning))
        {
            result.EstimatedRisk = RiskLevel.Medium;
        }
        else
        {
            result.EstimatedRisk = RiskLevel.Low;
        }

        return result;
    }
}

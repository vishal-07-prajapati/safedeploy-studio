using System.Text;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails.Rules;

public class ScriptSizeLimitRule : IGuardrailRule
{
    public string Name => "Script Size Limit Check";

    public Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var violations = new List<GuardrailViolation>();
        if (policy.MaxScriptSizeKb <= 0 || string.IsNullOrEmpty(sql))
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        var sizeBytes = Encoding.UTF8.GetByteCount(sql);
        var sizeKb = sizeBytes / 1024.0;

        if (sizeKb > policy.MaxScriptSizeKb)
        {
            violations.Add(new GuardrailViolation(
                "ScriptSizeLimit",
                GuardrailSeverity.Warning,
                $"Script payload size ({sizeKb:F1} KB) exceeds configured policy threshold ({policy.MaxScriptSizeKb} KB).",
                1,
                null,
                "Consider breaking large migration scripts into smaller, independently verifiable batches."
            ));
        }

        return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
    }
}

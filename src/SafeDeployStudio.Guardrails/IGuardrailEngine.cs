using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails;

public interface IGuardrailRule
{
    string Name { get; }
    Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default);
}

public interface IGuardrailEngine
{
    Task<GuardrailResult> ValidateScriptAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default);
}

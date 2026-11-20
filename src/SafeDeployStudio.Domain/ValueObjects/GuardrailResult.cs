using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.Domain.ValueObjects;

public record GuardrailViolation(
    string RuleName,
    GuardrailSeverity Severity,
    string Message,
    int LineNumber = 0,
    string? MatchedText = null,
    string? Remediation = null
);

public class GuardrailResult
{
    public bool IsValid => Violations.All(v => v.Severity != GuardrailSeverity.Error && v.Severity != GuardrailSeverity.Critical);
    public List<GuardrailViolation> Violations { get; set; } = [];
    public RiskLevel EstimatedRisk { get; set; } = RiskLevel.Low;

    public static GuardrailResult Success() => new() { EstimatedRisk = RiskLevel.Low };
}

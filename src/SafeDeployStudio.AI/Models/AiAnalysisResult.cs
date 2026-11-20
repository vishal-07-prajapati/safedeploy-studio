using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.AI.Models;

public record SargabilityWarning(
    string RuleId,
    string Title,
    string Description,
    int LineNumber,
    string MatchedText,
    string Recommendation,
    RiskLevel Severity = RiskLevel.Medium
);

public class AiAnalysisResult
{
    public string Summary { get; set; } = string.Empty;
    public string PerformanceImpactAnalysis { get; set; } = string.Empty;
    public string SecurityAndSafetyReview { get; set; } = string.Empty;
    public List<SargabilityWarning> SargabilityWarnings { get; set; } = [];
    public string? SuggestedOptimizedSql { get; set; }
    public RiskLevel AssessedRiskLevel { get; set; } = RiskLevel.Low;
}

public class AiProviderSettings
{
    public string Provider { get; set; } = "LocalRuleEngine"; // OpenAI, AzureOpenAI, Ollama, LocalRuleEngine
    public string ApiKey { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.1;
    public bool Enabled { get; set; } = false;
}

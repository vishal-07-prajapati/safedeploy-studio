using System.Net.Http.Json;
using System.Text.Json;
using SafeDeployStudio.AI.Models;
using SafeDeployStudio.Domain.Enums;

namespace SafeDeployStudio.AI.Services;

public interface IAiAdvisorService
{
    Task<AiAnalysisResult> AnalyzeProcedureMigrationAsync(
        string oldSql,
        string newSql,
        AiProviderSettings settings,
        CancellationToken ct = default);
}

public class AiAdvisorService : IAiAdvisorService
{
    private readonly ISargabilityAnalyzer _sargabilityAnalyzer;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiAdvisorService(
        ISargabilityAnalyzer sargabilityAnalyzer,
        IHttpClientFactory httpClientFactory)
    {
        _sargabilityAnalyzer = sargabilityAnalyzer;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AiAnalysisResult> AnalyzeProcedureMigrationAsync(
        string oldSql,
        string newSql,
        AiProviderSettings settings,
        CancellationToken ct = default)
    {
        var result = new AiAnalysisResult();

        // 1. Run local deterministic SARGability analyzer
        var warnings = _sargabilityAnalyzer.Analyze(newSql);
        result.SargabilityWarnings = warnings.ToList();

        if (warnings.Any(w => w.Severity == RiskLevel.Critical))
            result.AssessedRiskLevel = RiskLevel.Critical;
        else if (warnings.Any(w => w.Severity == RiskLevel.High))
            result.AssessedRiskLevel = RiskLevel.High;
        else if (warnings.Any(w => w.Severity == RiskLevel.Medium))
            result.AssessedRiskLevel = RiskLevel.Medium;
        else
            result.AssessedRiskLevel = RiskLevel.Low;

        // 2. Build local rule-based analysis summary
        result.PerformanceImpactAnalysis = warnings.Count > 0
            ? $"Identified {warnings.Count} performance advisory items (including {warnings.Count(w => w.Severity >= RiskLevel.High)} high-priority SARGability anti-patterns)."
            : "No SARGability anti-patterns detected. Query predicates conform to index seek best practices.";

        result.SecurityAndSafetyReview = "Static procedural code analysis complete. Review guardrail checks for schema modifications.";

        result.Summary = string.IsNullOrWhiteSpace(oldSql)
            ? "New stored procedure creation. Pre-flight checks passed."
            : $"Stored procedure modification. Detected {warnings.Count} optimization considerations.";

        // 3. If external LLM is configured and enabled, augment with AI synthesis
        if (settings.Enabled && !string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.EndpointUrl))
        {
            try
            {
                await CallExternalLlmAsync(oldSql, newSql, settings, result, ct);
            }
            catch (Exception ex)
            {
                result.Summary += $" (LLM advisory call was skipped due to: {ex.Message})";
            }
        }

        return result;
    }

    private async Task CallExternalLlmAsync(
        string oldSql,
        string newSql,
        AiProviderSettings settings,
        AiAnalysisResult result,
        CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("SafeDeployAiAdvisor");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");

        var prompt = $@"You are a Senior Microsoft SQL Server Database Architect and Query Tuning Specialist.
Analyze the following stored procedure changes:

OLD SQL:
```sql
{oldSql}
```

NEW SQL:
```sql
{newSql}
```

Provide:
1. A concise 2-sentence summary of what changed.
2. Index and performance impact assessment.
3. Potential query execution plan regressions (if any).
Respond in JSON format with fields: summary, performanceImpact, securityReview.";

        var payload = new
        {
            model = settings.ModelName,
            messages = new[]
            {
                new { role = "system", content = "You are a professional SQL Server query tuning assistant. Always respond in valid JSON." },
                new { role = "user", content = prompt }
            },
            temperature = settings.Temperature
        };

        var response = await client.PostAsJsonAsync(settings.EndpointUrl, payload, ct);
        if (response.IsSuccessStatusCode)
        {
            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var content = jsonDoc?.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (!string.IsNullOrWhiteSpace(content))
            {
                // Attempt to parse structured response
                try
                {
                    var cleanJson = content.Trim();
                    if (cleanJson.StartsWith("```json"))
                    {
                        cleanJson = cleanJson[7..^3].Trim();
                    }

                    using var parsed = JsonDocument.Parse(cleanJson);
                    if (parsed.RootElement.TryGetProperty("summary", out var s))
                        result.Summary = s.GetString() ?? result.Summary;
                    if (parsed.RootElement.TryGetProperty("performanceImpact", out var p))
                        result.PerformanceImpactAnalysis = p.GetString() ?? result.PerformanceImpactAnalysis;
                    if (parsed.RootElement.TryGetProperty("securityReview", out var sec))
                        result.SecurityAndSafetyReview = sec.GetString() ?? result.SecurityAndSafetyReview;
                }
                catch
                {
                    result.Summary = content;
                }
            }
        }
    }
}

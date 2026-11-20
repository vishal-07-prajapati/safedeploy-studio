using Microsoft.AspNetCore.Mvc;
using SafeDeployStudio.AI.Models;
using SafeDeployStudio.AI.Services;

namespace SafeDeployStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiAdvisorController : ControllerBase
{
    private readonly IAiAdvisorService _advisorService;

    public AiAdvisorController(IAiAdvisorService advisorService)
    {
        _advisorService = advisorService;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(
        [FromBody] AnalyzeProcedureRequest request,
        CancellationToken ct)
    {
        var settings = request.Settings ?? new AiProviderSettings();
        var result = await _advisorService.AnalyzeProcedureMigrationAsync(
            request.OldSql ?? string.Empty,
            request.NewSql,
            settings,
            ct);

        return Ok(result);
    }
}

public record AnalyzeProcedureRequest(
    string? OldSql,
    string NewSql,
    AiProviderSettings? Settings = null
);

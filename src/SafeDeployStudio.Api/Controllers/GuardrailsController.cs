using Microsoft.AspNetCore.Mvc;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Repositories;
using SafeDeployStudio.Guardrails;

namespace SafeDeployStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuardrailsController : ControllerBase
{
    private readonly IGuardrailPolicyRepository _guardrailRepo;
    private readonly IGuardrailEngine _guardrailEngine;

    public GuardrailsController(
        IGuardrailPolicyRepository guardrailRepo,
        IGuardrailEngine guardrailEngine)
    {
        _guardrailRepo = guardrailRepo;
        _guardrailEngine = guardrailEngine;
    }

    [HttpGet("policy/{projectId:guid}")]
    public async Task<IActionResult> GetPolicy(Guid projectId, CancellationToken ct)
    {
        var policy = await _guardrailRepo.GetByProjectIdAsync(projectId, ct);
        if (policy == null)
        {
            policy = new GuardrailPolicy(projectId, "Default SafeGuard Policy");
            await _guardrailRepo.AddAsync(policy, ct);
        }
        return Ok(policy);
    }

    [HttpPut("policy")]
    public async Task<IActionResult> UpdatePolicy([FromBody] GuardrailPolicy policy, CancellationToken ct)
    {
        await _guardrailRepo.UpdateAsync(policy, ct);
        return Ok(policy);
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateScript([FromBody] ValidateScriptRequest request, CancellationToken ct)
    {
        var policy = await _guardrailRepo.GetByProjectIdAsync(request.ProjectId, ct)
                     ?? new GuardrailPolicy(request.ProjectId, "Default SafeGuard Policy");

        var result = await _guardrailEngine.ValidateScriptAsync(request.SqlScript, policy, ct);
        return Ok(result);
    }
}

public record ValidateScriptRequest(Guid ProjectId, string SqlScript);

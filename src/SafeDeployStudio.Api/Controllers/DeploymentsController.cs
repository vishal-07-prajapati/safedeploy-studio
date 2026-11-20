using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SafeDeployStudio.Api.Hubs;
using SafeDeployStudio.Application.DTOs;
using SafeDeployStudio.Application.Services;
using SafeDeployStudio.Backup;
using SafeDeployStudio.Domain.Repositories;

namespace SafeDeployStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly IDeploymentRecordRepository _deploymentRepo;
    private readonly IBackupSnapshotRepository _snapshotRepo;
    private readonly IBackupVaultService _backupVaultService;
    private readonly IHubContext<DeploymentHub> _hubContext;

    public DeploymentsController(
        IWorkflowService workflowService,
        IDeploymentRecordRepository deploymentRepo,
        IBackupSnapshotRepository snapshotRepo,
        IBackupVaultService backupVaultService,
        IHubContext<DeploymentHub> hubContext)
    {
        _workflowService = workflowService;
        _deploymentRepo = deploymentRepo;
        _snapshotRepo = snapshotRepo;
        _backupVaultService = backupVaultService;
        _hubContext = hubContext;
    }

    [HttpPost("compare")]
    public async Task<IActionResult> CompareProcedure([FromBody] ProcedureCompareRequest request, CancellationToken ct)
    {
        var result = await _workflowService.CompareProcedureAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteDeployment([FromBody] ExecuteDeploymentCommand command, CancellationToken ct)
    {
        var record = await _workflowService.ExecuteDeploymentAsync(
            command,
            logMsg =>
            {
                _ = _hubContext.Clients.All.SendAsync("DeploymentLogReceived", logMsg, cancellationToken: CancellationToken.None);
            },
            ct);

        return Ok(record);
    }

    [HttpPost("rollback")]
    public async Task<IActionResult> Rollback([FromBody] RollbackCommand command, CancellationToken ct)
    {
        var record = await _workflowService.ExecuteRollbackAsync(command, ct);
        return Ok(record);
    }

    [HttpGet("history/{projectId:guid}")]
    public async Task<IActionResult> GetDeploymentHistory(Guid projectId, [FromQuery] int count = 50, CancellationToken ct = default)
    {
        var records = await _deploymentRepo.GetRecentByProjectIdAsync(projectId, count, ct);
        return Ok(records);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDeploymentDetails(Guid id, CancellationToken ct)
    {
        var record = await _deploymentRepo.GetByIdAsync(id, ct);
        if (record == null) return NotFound();
        return Ok(record);
    }

    [HttpGet("snapshots/{projectId:guid}")]
    public async Task<IActionResult> GetSnapshots(Guid projectId, [FromQuery] int count = 50, CancellationToken ct = default)
    {
        var snapshots = await _snapshotRepo.GetRecentByProjectIdAsync(projectId, count, ct);
        return Ok(snapshots);
    }

    [HttpGet("snapshots/{id:guid}/content")]
    public async Task<IActionResult> GetSnapshotContent(Guid id, CancellationToken ct)
    {
        var snapshot = await _snapshotRepo.GetByIdAsync(id, ct);
        if (snapshot == null) return NotFound();

        var content = await _backupVaultService.ReadSnapshotFileAsync(snapshot, ct);
        return Ok(new { content });
    }
}

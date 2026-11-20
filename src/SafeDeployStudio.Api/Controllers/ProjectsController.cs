using Microsoft.AspNetCore.Mvc;
using SafeDeployStudio.Application.DTOs;
using SafeDeployStudio.Application.Services;
using SafeDeployStudio.DatabaseProviders.Abstractions;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Repositories;

namespace SafeDeployStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IDatabaseConnectionRepository _connRepo;
    private readonly IDatabaseProviderFactory _providerFactory;
    private readonly IWorkflowService _workflowService;

    public ProjectsController(
        IOrganizationRepository orgRepo,
        IProjectRepository projectRepo,
        IDatabaseConnectionRepository connRepo,
        IDatabaseProviderFactory providerFactory,
        IWorkflowService workflowService)
    {
        _orgRepo = orgRepo;
        _projectRepo = projectRepo;
        _connRepo = connRepo;
        _providerFactory = providerFactory;
        _workflowService = workflowService;
    }

    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations(CancellationToken ct)
    {
        var orgs = await _orgRepo.GetAllAsync(ct);
        var dtos = orgs.Select(o => new OrganizationDto(o.Id, o.Name, o.Slug, o.Description, o.Projects.Count));
        return Ok(dtos);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] Guid? organizationId, CancellationToken ct)
    {
        if (organizationId.HasValue)
        {
            var projects = await _projectRepo.GetByOrganizationIdAsync(organizationId.Value, ct);
            return Ok(projects);
        }

        var orgs = await _orgRepo.GetAllAsync(ct);
        var allProjects = orgs.SelectMany(o => o.Projects).ToList();
        return Ok(allProjects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProjectById(Guid id, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(id, ct);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var project = new Project(request.OrganizationId, request.Name, request.Slug, request.Description);
        await _projectRepo.AddAsync(project, ct);
        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, project);
    }

    [HttpGet("connections/{projectId:guid}")]
    public async Task<IActionResult> GetConnections(Guid projectId, CancellationToken ct)
    {
        var conns = await _connRepo.GetByProjectIdAsync(projectId, ct);
        return Ok(conns);
    }

    [HttpPost("connections")]
    public async Task<IActionResult> CreateConnection([FromBody] CreateDatabaseConnectionRequest request, CancellationToken ct)
    {
        var conn = new DatabaseConnection(request.ProjectId, request.Name, request.Server, request.DatabaseName, request.ProviderType)
        {
            Port = request.Port,
            AuthType = request.AuthType,
            Username = request.Username,
            TrustServerCertificate = request.TrustServerCertificate,
            UseTlsSsl = request.UseTlsSsl,
            TimeoutSeconds = request.TimeoutSeconds
        };
        await _connRepo.AddAsync(conn, ct);
        return Ok(conn);
    }

    [HttpPost("connections/{id:guid}/test")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct)
    {
        var conn = await _connRepo.GetByIdAsync(id, ct);
        if (conn == null) return NotFound();

        var provider = _providerFactory.GetProvider(conn.ProviderType);
        var success = await provider.TestConnectionAsync(conn, ct);
        return Ok(new { isConnected = success });
    }

    [HttpGet("connections/{connectionId:guid}/procedures")]
    public async Task<IActionResult> GetProcedures(Guid connectionId, CancellationToken ct)
    {
        var procedures = await _workflowService.ListProceduresAsync(connectionId, ct);
        return Ok(procedures);
    }
}

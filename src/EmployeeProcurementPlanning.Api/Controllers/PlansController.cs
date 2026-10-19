using System.Security.Claims;
using EmployeeProcurementPlanning.Application.DTOs;
using EmployeeProcurementPlanning.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeProcurementPlanning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/plans")]
public sealed class PlansController(IProcurementPlanService service) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<DashboardDto> Dashboard(CancellationToken cancellationToken) =>
        service.DashboardAsync(CurrentUserId, CurrentRoles, cancellationToken);

    [HttpGet]
    public Task<PagedResult<PlanDto>> Get(
        [FromQuery] PlanQuery request,
        CancellationToken cancellationToken) =>
        service.SearchAsync(request, CurrentUserId, CurrentRoles, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<PlanDto> Get(Guid id, CancellationToken cancellationToken) =>
        service.GetAsync(id, CurrentUserId, CurrentRoles, cancellationToken);

    [HttpPost]
    public Task<PlanDto> Create(
        CreatePlanRequest request,
        CancellationToken cancellationToken) =>
        service.CreateAsync(request, CurrentUserId, cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<PlanDto> Update(
        Guid id,
        UpdatePlanRequest request,
        CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, CurrentUserId, CurrentRoles, cancellationToken);

    [HttpPost("{id:guid}/submit")]
    public Task<PlanDto> Submit(
        Guid id,
        WorkflowRequest request,
        CancellationToken cancellationToken) =>
        service.TransitionAsync(id, "submit", request, CurrentUserId, CurrentRoles, cancellationToken);

    [Authorize(Roles = "Administrator,Manager")]
    [HttpPost("{id:guid}/review")]
    public Task<PlanDto> Review(
        Guid id,
        WorkflowRequest request,
        CancellationToken cancellationToken) =>
        service.TransitionAsync(id, "review", request, CurrentUserId, CurrentRoles, cancellationToken);

    [Authorize(Roles = "Administrator,Manager")]
    [HttpPost("{id:guid}/approve")]
    public Task<PlanDto> Approve(
        Guid id,
        WorkflowRequest request,
        CancellationToken cancellationToken) =>
        service.TransitionAsync(id, "approve", request, CurrentUserId, CurrentRoles, cancellationToken);

    [Authorize(Roles = "Administrator,Manager")]
    [HttpPost("{id:guid}/reject")]
    public Task<PlanDto> Reject(
        Guid id,
        WorkflowRequest request,
        CancellationToken cancellationToken) =>
        service.TransitionAsync(id, "reject", request, CurrentUserId, CurrentRoles, cancellationToken);

    [Authorize(Roles = "Administrator,Manager")]
    [HttpPost("{id:guid}/return")]
    public Task<PlanDto> Return(
        Guid id,
        WorkflowRequest request,
        CancellationToken cancellationToken) =>
        service.TransitionAsync(id, "return", request, CurrentUserId, CurrentRoles, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    public Task<PlanDto> Cancel(
        Guid id,
        WorkflowRequest request,
        CancellationToken cancellationToken) =>
        service.TransitionAsync(id, "cancel", request, CurrentUserId, CurrentRoles, cancellationToken);

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("The authenticated user identifier is invalid.");

    private string[] CurrentRoles =>
        User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();
}

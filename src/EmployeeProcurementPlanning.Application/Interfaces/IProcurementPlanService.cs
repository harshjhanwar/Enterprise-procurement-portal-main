using EmployeeProcurementPlanning.Application.DTOs;

namespace EmployeeProcurementPlanning.Application.Interfaces;

public interface IProcurementPlanService
{
    Task<PagedResult<PlanDto>> SearchAsync(
        PlanQuery request,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken);

    Task<PlanDto> GetAsync(
        Guid id,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken);

    Task<PlanDto> CreateAsync(
        CreatePlanRequest request,
        Guid userId,
        CancellationToken cancellationToken);

    Task<PlanDto> UpdateAsync(
        Guid id,
        UpdatePlanRequest request,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken);

    Task<PlanDto> TransitionAsync(
        Guid id,
        string action,
        WorkflowRequest request,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken);

    Task<DashboardDto> DashboardAsync(
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken);
}

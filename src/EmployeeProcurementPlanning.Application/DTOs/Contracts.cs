using EmployeeProcurementPlanning.Domain.Enums;

namespace EmployeeProcurementPlanning.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string DisplayName,
    string[] Roles);

public sealed record PlanItemDto(
    Guid Id,
    string Category,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    DateOnly RequiredPeriod,
    Priority Priority,
    string Justification,
    string? VendorPreference,
    LineItemStatus Status,
    decimal EstimatedTotalCost);

public sealed record PlanDto(
    Guid Id,
    string PlanNumber,
    string Title,
    Guid DepartmentId,
    Guid RequesterId,
    Guid? ManagerId,
    DateOnly PlanningPeriodStart,
    DateOnly PlanningPeriodEnd,
    Priority Priority,
    PlanStatus Status,
    string BusinessJustification,
    decimal EstimatedBudget,
    DateTime RequestedDateUtc,
    string? Notes,
    string RowVersion,
    IReadOnlyList<PlanItemDto> Items,
    IReadOnlyList<AuditDto> History);

public sealed record CreatePlanRequest(
    string Title,
    Guid DepartmentId,
    Guid? ManagerId,
    DateOnly PlanningPeriodStart,
    DateOnly PlanningPeriodEnd,
    Priority Priority,
    string BusinessJustification,
    decimal EstimatedBudget,
    DateTime RequestedDateUtc,
    string? Notes,
    List<CreatePlanItemRequest> Items);

public sealed record CreatePlanItemRequest(
    string Category,
    string Description,
    decimal Quantity,
    decimal EstimatedUnitCost,
    DateOnly RequiredPeriod,
    Priority Priority,
    string Justification,
    string? VendorPreference);

public sealed record UpdatePlanRequest(
    string Title,
    Guid DepartmentId,
    Guid? ManagerId,
    DateOnly PlanningPeriodStart,
    DateOnly PlanningPeriodEnd,
    Priority Priority,
    string BusinessJustification,
    decimal EstimatedBudget,
    DateTime RequestedDateUtc,
    string? Notes,
    List<CreatePlanItemRequest> Items,
    string RowVersion);

public sealed record WorkflowRequest(string? Comment, string RowVersion);

public sealed record PlanQuery(
    string? Search,
    PlanStatus? Status,
    Guid? DepartmentId,
    Priority? Priority,
    DateOnly? From,
    DateOnly? To,
    int Page = 1,
    int PageSize = 20);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record DashboardDto(
    int TotalPlans,
    int DraftPlans,
    int SubmittedPlans,
    int AwaitingApproval,
    int ApprovedPlans,
    int RejectedReturnedPlans,
    decimal EstimatedTotalBudget,
    IReadOnlyList<TrendPoint> Trends,
    IReadOnlyList<AuditDto> RecentActivity);

public sealed record TrendPoint(string Label, int Count, decimal Budget);

public sealed record AuditDto(
    Guid Id,
    string Action,
    string? PreviousStatus,
    string? NewStatus,
    string? Comment,
    DateTime CreatedAtUtc,
    string? ActorName);

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? DepartmentName,
    string[] Roles,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record UpdateUserRequest(string[] Roles, bool IsActive);

using System.Linq.Expressions;
using EmployeeProcurementPlanning.Application.DTOs;
using EmployeeProcurementPlanning.Application.Interfaces;
using EmployeeProcurementPlanning.Application.Validators;
using EmployeeProcurementPlanning.Domain.Entities;
using EmployeeProcurementPlanning.Domain.Enums;
using EmployeeProcurementPlanning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeProcurementPlanning.Infrastructure.Services;

public sealed class ProcurementPlanService(AppDbContext db) : IProcurementPlanService
{
    private static bool IsAdmin(IReadOnlyCollection<string> roles) =>
        roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase);

    private static bool IsManager(IReadOnlyCollection<string> roles) =>
        roles.Contains("Manager", StringComparer.OrdinalIgnoreCase);

    private static bool CanReview(IReadOnlyCollection<string> roles) =>
        IsManager(roles) || IsAdmin(roles);

    private IQueryable<ProcurementPlan> Scope(Guid userId, IReadOnlyCollection<string> roles)
    {
        var query = db.ProcurementPlans.AsQueryable();

        if (IsAdmin(roles))
        {
            return query;
        }

        if (IsManager(roles))
        {
            return query.Where(plan =>
                plan.ManagerId == userId ||
                plan.DepartmentId == db.Users
                    .Where(user => user.Id == userId)
                    .Select(user => user.DepartmentId)
                    .FirstOrDefault());
        }

        return query.Where(plan => plan.RequesterId == userId);
    }

    public async Task<PagedResult<PlanDto>> SearchAsync(
        PlanQuery request,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = Scope(userId, roles)
            .AsNoTracking()
            .Include(plan => plan.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(plan =>
                plan.PlanNumber.Contains(search) ||
                plan.Title.Contains(search));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(plan => plan.Status == request.Status.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(plan => plan.DepartmentId == request.DepartmentId.Value);
        }

        if (request.Priority.HasValue)
        {
            query = query.Where(plan => plan.Priority == request.Priority.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(plan => plan.PlanningPeriodStart >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(plan => plan.PlanningPeriodEnd <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var plans = await query
            .OrderByDescending(plan => plan.UpdatedAtUtc)
            .ThenBy(plan => plan.PlanNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = plans.Select(plan => Map(plan, Array.Empty<AuditDto>())).ToList();

        return new PagedResult<PlanDto>(items, page, pageSize, totalCount);
    }

    public async Task<PlanDto> GetAsync(
        Guid id,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken)
    {
        var plan = await Scope(userId, roles)
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Procurement plan not found.");

        return await MapWithHistoryAsync(plan, cancellationToken);
    }

    public async Task<PlanDto> CreateAsync(
        CreatePlanRequest request,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await ValidatePlanAsync(request, cancellationToken);

        var now = DateTime.UtcNow;
        var plan = new ProcurementPlan
        {
            Id = Guid.NewGuid(),
            PlanNumber = await GeneratePlanNumberAsync(now.Year, cancellationToken),
            Title = request.Title.Trim(),
            DepartmentId = request.DepartmentId,
            RequesterId = userId,
            ManagerId = request.ManagerId,
            PlanningPeriodStart = request.PlanningPeriodStart,
            PlanningPeriodEnd = request.PlanningPeriodEnd,
            Priority = request.Priority,
            Status = PlanStatus.Draft,
            BusinessJustification = request.BusinessJustification.Trim(),
            EstimatedBudget = request.EstimatedBudget,
            RequestedDateUtc = request.RequestedDateUtc.Kind == DateTimeKind.Utc
                ? request.RequestedDateUtc
                : request.RequestedDateUtc.ToUniversalTime(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var item in request.Items)
        {
            plan.Items.Add(ToEntity(item));
        }

        AddAudit(plan, userId, "Created", null, PlanStatus.Draft, null);

        db.ProcurementPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        return await MapWithHistoryAsync(plan, cancellationToken);
    }

    public async Task<PlanDto> UpdateAsync(
        Guid id,
        UpdatePlanRequest request,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken)
    {
        await ValidatePlanAsync(
            new CreatePlanRequest(
                request.Title,
                request.DepartmentId,
                request.ManagerId,
                request.PlanningPeriodStart,
                request.PlanningPeriodEnd,
                request.Priority,
                request.BusinessJustification,
                request.EstimatedBudget,
                request.RequestedDateUtc,
                request.Notes,
                request.Items),
            cancellationToken);

        var plan = await Scope(userId, roles)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Procurement plan not found.");

        if (plan.RequesterId != userId && !IsAdmin(roles) && !IsManager(roles))
        {
            throw new UnauthorizedAccessException("You are not allowed to edit this procurement plan.");
        }

        if (plan.Status is not (PlanStatus.Draft or PlanStatus.ReturnedForCorrection))
        {
            throw new InvalidOperationException("Only draft or returned plans can be edited.");
        }

        ApplyConcurrencyToken(plan, request.RowVersion);

        plan.Title = request.Title.Trim();
        plan.DepartmentId = request.DepartmentId;
        plan.ManagerId = request.ManagerId;
        plan.PlanningPeriodStart = request.PlanningPeriodStart;
        plan.PlanningPeriodEnd = request.PlanningPeriodEnd;
        plan.Priority = request.Priority;
        plan.BusinessJustification = request.BusinessJustification.Trim();
        plan.EstimatedBudget = request.EstimatedBudget;
        plan.RequestedDateUtc = request.RequestedDateUtc.Kind == DateTimeKind.Utc
            ? request.RequestedDateUtc
            : request.RequestedDateUtc.ToUniversalTime();
        plan.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        plan.UpdatedAtUtc = DateTime.UtcNow;

        db.ProcurementPlanItems.RemoveRange(plan.Items);
        plan.Items.Clear();

        foreach (var item in request.Items)
        {
            plan.Items.Add(ToEntity(item));
        }

        AddAudit(plan, userId, "Updated", plan.Status, plan.Status, null);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This procurement plan was changed by another user. Reload the plan and try again.");
        }

        return await MapWithHistoryAsync(plan, cancellationToken);
    }

    public async Task<PlanDto> TransitionAsync(
        Guid id,
        string action,
        WorkflowRequest request,
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken)
    {
        var plan = await Scope(userId, roles)
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Procurement plan not found.");

        var nextStatus = action.ToLowerInvariant() switch
        {
            "submit" when (plan.Status is PlanStatus.Draft or PlanStatus.ReturnedForCorrection)
                && plan.RequesterId == userId
                => PlanStatus.Submitted,
            "review" when CanReview(roles) && plan.Status == PlanStatus.Submitted
                => PlanStatus.UnderReview,
            "approve" when CanReview(roles) && (plan.Status is PlanStatus.Submitted or PlanStatus.UnderReview)
                => PlanStatus.Approved,
            "reject" when CanReview(roles) && (plan.Status is PlanStatus.Submitted or PlanStatus.UnderReview)
                => PlanStatus.Rejected,
            "return" when CanReview(roles) && (plan.Status is PlanStatus.Submitted or PlanStatus.UnderReview)
                => PlanStatus.ReturnedForCorrection,
            "cancel" when (plan.Status is PlanStatus.Draft or PlanStatus.Submitted)
                && (plan.RequesterId == userId || IsAdmin(roles) || IsManager(roles))
                => PlanStatus.Cancelled,
            _ => throw new InvalidOperationException("The requested workflow transition is not allowed.")
        };

        if ((nextStatus is PlanStatus.Rejected or PlanStatus.ReturnedForCorrection) &&
            string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ArgumentException("A comment is required when rejecting or returning a plan.");
        }

        ApplyConcurrencyToken(plan, request.RowVersion);

        var previousStatus = plan.Status;
        plan.Status = nextStatus;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        AddAudit(
            plan,
            userId,
            ActionName(nextStatus),
            previousStatus,
            nextStatus,
            request.Comment?.Trim());

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This procurement plan was changed by another user. Reload the plan and try again.");
        }

        return await MapWithHistoryAsync(plan, cancellationToken);
    }

    public async Task<DashboardDto> DashboardAsync(
        Guid userId,
        string[] roles,
        CancellationToken cancellationToken)
    {
        var query = Scope(userId, roles).AsNoTracking();

        var total = await query.CountAsync(cancellationToken);
        var counts = await query
            .GroupBy(plan => plan.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var budget = await query
            .SumAsync(plan => (decimal?)plan.EstimatedBudget, cancellationToken) ?? 0m;

        var trends = await query
            .GroupBy(plan => plan.CreatedAtUtc.Date)
            .OrderByDescending(group => group.Key)
            .Take(8)
            .Select(group => new TrendPoint(
                group.Key.ToString("yyyy-MM-dd"),
                group.Count(),
                group.Sum(plan => plan.EstimatedBudget)))
            .ToListAsync(cancellationToken);

        var recentActivity = await db.AuditEntries
            .AsNoTracking()
            .Where(entry => query.Select(plan => plan.Id).Contains(entry.ProcurementPlanId))
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(10)
            .Join(
                db.Users.AsNoTracking(),
                entry => entry.ActorUserId,
                user => (Guid?)user.Id,
                (entry, user) => new AuditDto(
                    entry.Id,
                    entry.Action,
                    entry.PreviousStatus,
                    entry.NewStatus,
                    entry.Comment,
                    entry.CreatedAtUtc,
                    user.DisplayName))
            .ToListAsync(cancellationToken);

        int Count(PlanStatus status) => counts.FirstOrDefault(item => item.Status == status)?.Count ?? 0;

        return new DashboardDto(
            total,
            Count(PlanStatus.Draft),
            Count(PlanStatus.Submitted),
            Count(PlanStatus.UnderReview),
            Count(PlanStatus.Approved),
            Count(PlanStatus.Rejected) + Count(PlanStatus.ReturnedForCorrection),
            budget,
            trends.OrderBy(point => point.Label).ToList(),
            recentActivity);
    }

    private async Task ValidatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        var validation = await new CreatePlanValidator().ValidateAsync(request, cancellationToken);

        if (!validation.IsValid)
        {
            throw new ArgumentException(string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
        }

        if (request.Items.Any(item => item.RequiredPeriod < request.PlanningPeriodStart ||
                                      item.RequiredPeriod > request.PlanningPeriodEnd))
        {
            throw new ArgumentException("Each line item's required period must fall within the planning period.");
        }

        if (!await db.Departments.AnyAsync(
                department => department.Id == request.DepartmentId && department.IsActive,
                cancellationToken))
        {
            throw new ArgumentException("The selected department is not active or does not exist.");
        }

        if (request.ManagerId.HasValue)
        {
            var managerIsValid = await db.Users
                .Where(user => user.Id == request.ManagerId.Value && user.IsActive)
                .SelectMany(user => db.UserRoles
                    .Where(role => role.UserId == user.Id)
                    .Join(
                        db.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => role.Name))
                .AnyAsync(role => role == "Manager" || role == "Administrator", cancellationToken);

            if (!managerIsValid)
            {
                throw new ArgumentException("The selected manager is not an active manager or administrator.");
            }
        }
    }

    private async Task<string> GeneratePlanNumberAsync(int year, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = $"PP-{year}-{Random.Shared.Next(100000, 999999)}";
            if (!await db.ProcurementPlans.AnyAsync(plan => plan.PlanNumber == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique procurement plan number.");
    }

    private async Task<PlanDto> MapWithHistoryAsync(
        ProcurementPlan plan,
        CancellationToken cancellationToken)
    {
        var history = await db.AuditEntries
            .AsNoTracking()
            .Where(entry => entry.ProcurementPlanId == plan.Id)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Join(
                db.Users.AsNoTracking(),
                entry => entry.ActorUserId,
                user => (Guid?)user.Id,
                (entry, user) => new AuditDto(
                    entry.Id,
                    entry.Action,
                    entry.PreviousStatus,
                    entry.NewStatus,
                    entry.Comment,
                    entry.CreatedAtUtc,
                    user.DisplayName))
            .ToListAsync(cancellationToken);

        return Map(plan, history);
    }

    private static ProcurementPlanItem ToEntity(CreatePlanItemRequest request) => new()
    {
        Id = Guid.NewGuid(),
        Category = request.Category.Trim(),
        Description = request.Description.Trim(),
        Quantity = request.Quantity,
        EstimatedUnitCost = request.EstimatedUnitCost,
        EstimatedTotalCost = request.Quantity * request.EstimatedUnitCost,
        RequiredPeriod = request.RequiredPeriod,
        Priority = request.Priority,
        Justification = request.Justification.Trim(),
        VendorPreference = string.IsNullOrWhiteSpace(request.VendorPreference)
            ? null
            : request.VendorPreference.Trim(),
        Status = LineItemStatus.Draft
    };

    private static void AddAudit(
        ProcurementPlan plan,
        Guid userId,
        string action,
        PlanStatus? previousStatus,
        PlanStatus? newStatus,
        string? comment)
    {
        plan.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = userId,
            Action = action,
            PreviousStatus = previousStatus?.ToString(),
            NewStatus = newStatus?.ToString(),
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static string ActionName(PlanStatus status) => status switch
    {
        PlanStatus.Submitted => "Submitted",
        PlanStatus.UnderReview => "Under Review",
        PlanStatus.Approved => "Approved",
        PlanStatus.Rejected => "Rejected",
        PlanStatus.ReturnedForCorrection => "Returned for correction",
        PlanStatus.Cancelled => "Cancelled",
        _ => "Updated"
    };

    private void ApplyConcurrencyToken(ProcurementPlan plan, string rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new ArgumentException("A row version is required for this operation.");
        }

        byte[] original;
        try
        {
            original = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ArgumentException("The row version is invalid.");
        }

        db.Entry(plan).Property(item => item.RowVersion).OriginalValue = original;
    }

    private static PlanDto Map(ProcurementPlan plan, IReadOnlyList<AuditDto> history) => new(
        plan.Id,
        plan.PlanNumber,
        plan.Title,
        plan.DepartmentId,
        plan.RequesterId,
        plan.ManagerId,
        plan.PlanningPeriodStart,
        plan.PlanningPeriodEnd,
        plan.Priority,
        plan.Status,
        plan.BusinessJustification,
        plan.EstimatedBudget,
        plan.RequestedDateUtc,
        plan.Notes,
        Convert.ToBase64String(plan.RowVersion),
        plan.Items
            .Select(item => new PlanItemDto(
                item.Id,
                item.Category,
                item.Description,
                item.Quantity,
                item.EstimatedUnitCost,
                item.RequiredPeriod,
                item.Priority,
                item.Justification,
                item.VendorPreference,
                item.Status,
                item.EstimatedTotalCost))
            .ToList(),
        history);
}

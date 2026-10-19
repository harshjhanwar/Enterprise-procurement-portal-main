using EmployeeProcurementPlanning.Domain.Enums;

namespace EmployeeProcurementPlanning.Domain.Entities;

public sealed class ProcurementPlan
{
    public Guid Id { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid? ManagerId { get; set; }
    public DateOnly PlanningPeriodStart { get; set; }
    public DateOnly PlanningPeriodEnd { get; set; }
    public Priority Priority { get; set; }
    public PlanStatus Status { get; set; } = PlanStatus.Draft;
    public string BusinessJustification { get; set; } = string.Empty;
    public decimal EstimatedBudget { get; set; }
    public DateTime RequestedDateUtc { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Department? Department { get; set; }
    public ICollection<ProcurementPlanItem> Items { get; set; } = new List<ProcurementPlanItem>();
    public ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();
}

namespace EmployeeProcurementPlanning.Domain.Entities;

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public Guid ProcurementPlanId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Comment { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ProcurementPlan? ProcurementPlan { get; set; }
}

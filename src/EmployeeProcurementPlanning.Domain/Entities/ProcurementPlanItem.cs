using EmployeeProcurementPlanning.Domain.Enums;

namespace EmployeeProcurementPlanning.Domain.Entities;

public sealed class ProcurementPlanItem
{
    public Guid Id { get; set; }
    public Guid ProcurementPlanId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedTotalCost { get; set; }
    public DateOnly RequiredPeriod { get; set; }
    public Priority Priority { get; set; }
    public string Justification { get; set; } = string.Empty;
    public string? VendorPreference { get; set; }
    public LineItemStatus Status { get; set; } = LineItemStatus.Draft;

    public ProcurementPlan? ProcurementPlan { get; set; }
}

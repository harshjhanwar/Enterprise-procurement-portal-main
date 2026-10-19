namespace EmployeeProcurementPlanning.Domain.Enums;

public enum PlanStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    ReturnedForCorrection,
    Cancelled
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum LineItemStatus
{
    Draft,
    Requested,
    Approved,
    Rejected
}

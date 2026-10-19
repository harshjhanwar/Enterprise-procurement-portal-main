using EmployeeProcurementPlanning.Domain.Enums;
using Xunit;

namespace EmployeeProcurementPlanning.IntegrationTests;

public sealed class SmokeTests
{
    [Fact]
    public void WorkflowStatusesContainRequiredStates()
    {
        var statuses = Enum.GetValues<PlanStatus>();

        Assert.Contains(PlanStatus.Draft, statuses);
        Assert.Contains(PlanStatus.Submitted, statuses);
        Assert.Contains(PlanStatus.UnderReview, statuses);
        Assert.Contains(PlanStatus.Approved, statuses);
        Assert.Contains(PlanStatus.Rejected, statuses);
        Assert.Contains(PlanStatus.ReturnedForCorrection, statuses);
        Assert.Contains(PlanStatus.Cancelled, statuses);
    }
}

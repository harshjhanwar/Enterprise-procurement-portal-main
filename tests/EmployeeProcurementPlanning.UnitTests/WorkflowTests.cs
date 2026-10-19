using EmployeeProcurementPlanning.Domain.Entities;
using EmployeeProcurementPlanning.Domain.Enums;
using Xunit;

namespace EmployeeProcurementPlanning.UnitTests;

public sealed class WorkflowTests
{
    [Fact]
    public void NewPlanStartsAsDraft()
    {
        var plan = new ProcurementPlan();

        Assert.Equal(PlanStatus.Draft, plan.Status);
    }

    [Fact]
    public void ItemTotalIsQuantityTimesUnitCost()
    {
        var item = new ProcurementPlanItem
        {
            Quantity = 3,
            EstimatedUnitCost = 125m
        };

        item.EstimatedTotalCost = item.Quantity * item.EstimatedUnitCost;

        Assert.Equal(375m, item.EstimatedTotalCost);
    }

    [Fact]
    public void ProcurementPlanStartsWithEmptyItemsAndHistory()
    {
        var plan = new ProcurementPlan();

        Assert.Empty(plan.Items);
        Assert.Empty(plan.AuditEntries);
    }
}

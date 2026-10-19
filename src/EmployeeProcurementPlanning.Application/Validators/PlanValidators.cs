using EmployeeProcurementPlanning.Application.DTOs;
using FluentValidation;

namespace EmployeeProcurementPlanning.Application.Validators;

public sealed class CreatePlanValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.BusinessJustification)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(request => request.EstimatedBudget)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.PlanningPeriodEnd)
            .GreaterThanOrEqualTo(request => request.PlanningPeriodStart);

        RuleFor(request => request.Items)
            .NotEmpty()
            .Must(items => items.Count <= 100)
            .WithMessage("A plan can contain at most 100 line items.");

        RuleForEach(request => request.Items)
            .SetValidator(new CreatePlanItemValidator());
    }
}

public sealed class CreatePlanItemValidator : AbstractValidator<CreatePlanItemRequest>
{
    public CreatePlanItemValidator()
    {
        RuleFor(item => item.Category)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(item => item.Description)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(item => item.Quantity)
            .GreaterThan(0);

        RuleFor(item => item.EstimatedUnitCost)
            .GreaterThanOrEqualTo(0);

        RuleFor(item => item.Justification)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(item => item.VendorPreference)
            .MaximumLength(200);
    }
}

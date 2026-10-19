using EmployeeProcurementPlanning.Domain.Entities;
using EmployeeProcurementPlanning.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeProcurementPlanning.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var db = serviceProvider.GetRequiredService<AppDbContext>();

        if (bool.TryParse(configuration["Database:ApplyMigrationsOnStartup"], out var applyMigrationsOnStartup) && applyMigrationsOnStartup)
        {
            await db.Database.MigrateAsync();
        }

        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in new[] { "Administrator", "Manager", "Employee" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                EnsureSuccess(result, $"Unable to create role '{roleName}'.");
            }
        }

        var departments = await EnsureDepartmentsAsync(db);
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var admin = await EnsureConfiguredUserAsync(
            userManager,
            configuration["BootstrapAdmin:Email"],
            configuration["BootstrapAdmin:Password"],
            configuration["BootstrapAdmin:DisplayName"] ?? "System Administrator",
            "Administrator",
            null);

        var manager = await EnsureConfiguredUserAsync(
            userManager,
            configuration["DemoUsers:Manager:Email"],
            configuration["DemoUsers:Manager:Password"],
            "Demo Manager",
            "Manager",
            departments["IT"]);

        var employee = await EnsureConfiguredUserAsync(
            userManager,
            configuration["DemoUsers:Employee:Email"],
            configuration["DemoUsers:Employee:Password"],
            "Demo Employee",
            "Employee",
            departments["IT"]);

        if (bool.TryParse(configuration["SeedDemoData"], out var seedDemoData) 
    && seedDemoData 
    && manager is not null 
    && employee is not null)
        {
            await EnsureDemoPlansAsync(db, manager, employee, departments["IT"]);
        }
    }

    private static async Task<Dictionary<string, Guid>> EnsureDepartmentsAsync(AppDbContext db)
    {
        var defaults = new[]
        {
            (Name: "Information Technology", Code: "IT"),
            (Name: "Finance", Code: "FIN"),
            (Name: "Human Resources", Code: "HR"),
            (Name: "Operations", Code: "OPS")
        };

        foreach (var item in defaults)
        {
            var department = await db.Departments.SingleOrDefaultAsync(x => x.Code == item.Code);
            if (department is null)
            {
                db.Departments.Add(new Department
                {
                    Id = Guid.NewGuid(),
                    Name = item.Name,
                    Code = item.Code,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();

        return await db.Departments
            .Where(x => defaults.Select(d => d.Code).Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, x => x.Id);
    }

    private static async Task<ApplicationUser?> EnsureConfiguredUserAsync(
        UserManager<ApplicationUser> userManager,
        string? email,
        string? password,
        string displayName,
        string role,
        Guid? departmentId)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        email = email.Trim();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                DepartmentId = departmentId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSuccess(createResult, $"Unable to create bootstrap user '{email}'.");
        }
        else
        {
            var changed = false;

            if (user.DisplayName != displayName)
            {
                user.DisplayName = displayName;
                changed = true;
            }

            if (user.DepartmentId != departmentId)
            {
                user.DepartmentId = departmentId;
                changed = true;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                changed = true;
            }

            if (changed)
            {
                var updateResult = await userManager.UpdateAsync(user);
                EnsureSuccess(updateResult, $"Unable to update bootstrap user '{email}'.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            EnsureSuccess(roleResult, $"Unable to assign role '{role}' to '{email}'.");
        }

        return user;
    }

    private static async Task EnsureDemoPlansAsync(
        AppDbContext db,
        ApplicationUser manager,
        ApplicationUser employee,
        Guid departmentId)
    {
        if (await db.ProcurementPlans.AnyAsync())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = new ProcurementPlan
        {
            Id = Guid.NewGuid(),
            PlanNumber = $"DEMO-{today:yyyy}-001",
            Title = "Developer laptop refresh",
            DepartmentId = departmentId,
            RequesterId = employee.Id,
            ManagerId = manager.Id,
            PlanningPeriodStart = today.AddMonths(1),
            PlanningPeriodEnd = today.AddMonths(3),
            Priority = Priority.High,
            Status = PlanStatus.Submitted,
            BusinessJustification = "Replace end-of-life development laptops for the engineering team.",
            EstimatedBudget = 480000m,
            RequestedDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            Items =
            {
                new ProcurementPlanItem
                {
                    Id = Guid.NewGuid(),
                    Category = "Hardware",
                    Description = "Developer laptop",
                    Quantity = 8,
                    EstimatedUnitCost = 60000m,
                    EstimatedTotalCost = 480000m,
                    RequiredPeriod = today.AddMonths(1),
                    Priority = Priority.High,
                    Justification = "Replace devices reaching end of support.",
                    Status = LineItemStatus.Requested
                }
            },
            AuditEntries =
            {
                new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = employee.Id,
                    Action = "Created",
                    NewStatus = PlanStatus.Draft.ToString(),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = employee.Id,
                    Action = "Submitted",
                    PreviousStatus = PlanStatus.Draft.ToString(),
                    NewStatus = PlanStatus.Submitted.ToString(),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
                }
            }
        };

        var second = new ProcurementPlan
        {
            Id = Guid.NewGuid(),
            PlanNumber = $"DEMO-{today:yyyy}-002",
            Title = "Cloud observability subscription",
            DepartmentId = departmentId,
            RequesterId = employee.Id,
            ManagerId = manager.Id,
            PlanningPeriodStart = today.AddMonths(2),
            PlanningPeriodEnd = today.AddMonths(14),
            Priority = Priority.Medium,
            Status = PlanStatus.Draft,
            BusinessJustification = "Standardize monitoring and alerting for production workloads.",
            EstimatedBudget = 180000m,
            RequestedDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-3),
            Items =
            {
                new ProcurementPlanItem
                {
                    Id = Guid.NewGuid(),
                    Category = "SaaS subscriptions",
                    Description = "Observability platform subscription",
                    Quantity = 12,
                    EstimatedUnitCost = 15000m,
                    EstimatedTotalCost = 180000m,
                    RequiredPeriod = today.AddMonths(2),
                    Priority = Priority.Medium,
                    Justification = "Provide centralized monitoring for production services.",
                    Status = LineItemStatus.Draft
                }
            },
            AuditEntries =
            {
                new AuditEntry
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = employee.Id,
                    Action = "Created",
                    NewStatus = PlanStatus.Draft.ToString(),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
                }
            }
        };

        db.ProcurementPlans.AddRange(first, second);
        await db.SaveChangesAsync();
    }

    private static void EnsureSuccess(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"{message} {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}

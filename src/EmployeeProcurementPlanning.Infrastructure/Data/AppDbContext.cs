using EmployeeProcurementPlanning.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EmployeeProcurementPlanning.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ProcurementPlan> ProcurementPlans => Set<ProcurementPlan>();
    public DbSet<ProcurementPlanItem> ProcurementPlanItems => Set<ProcurementPlanItem>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Code).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Code).HasMaxLength(30).IsRequired();
            entity.Property(item => item.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(item => item.DisplayName).HasMaxLength(150).IsRequired();
            entity.HasIndex(item => item.DepartmentId);
            entity.HasOne<Department>()
                .WithMany()
                .HasForeignKey(item => item.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProcurementPlan>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.PlanNumber).IsUnique();
            entity.HasIndex(item => new { item.DepartmentId, item.Status });
            entity.HasIndex(item => new { item.RequesterId, item.CreatedAtUtc });
            entity.HasIndex(item => new { item.ManagerId, item.Status });

            entity.Property(item => item.PlanNumber).HasMaxLength(50).IsRequired();
            entity.Property(item => item.PlanNumber).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Title).HasMaxLength(200).IsRequired();
            entity.Property(item => item.BusinessJustification).HasMaxLength(4000).IsRequired();
            entity.Property(item => item.Notes).HasMaxLength(4000);
            entity.Property(item => item.EstimatedBudget).HasPrecision(19, 4);
            entity.Property(item => item.RowVersion).IsRowVersion();

            entity.HasOne(item => item.Department)
                .WithMany()
                .HasForeignKey(item => item.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(item => item.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(item => item.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(item => item.Items)
                .WithOne(item => item.ProcurementPlan)
                .HasForeignKey(item => item.ProcurementPlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcurementPlanItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.ProcurementPlanId);
            entity.Property(item => item.Category).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(500).IsRequired();
            entity.Property(item => item.Quantity).HasPrecision(19, 4);
            entity.Property(item => item.EstimatedUnitCost).HasPrecision(19, 4);
            entity.Property(item => item.EstimatedTotalCost).HasPrecision(19, 4);
            entity.Property(item => item.Justification).HasMaxLength(2000).IsRequired();
            entity.Property(item => item.VendorPreference).HasMaxLength(200);
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.ProcurementPlanId, item.CreatedAtUtc });
            entity.Property(item => item.Action).HasMaxLength(80).IsRequired();
            entity.Property(item => item.PreviousStatus).HasMaxLength(80);
            entity.Property(item => item.NewStatus).HasMaxLength(80);
            entity.Property(item => item.Comment).HasMaxLength(2000);
            entity.Property(item => item.MetadataJson).HasMaxLength(8000);

            entity.HasOne(item => item.ProcurementPlan)
                .WithMany(plan => plan.AuditEntries)
                .HasForeignKey(item => item.ProcurementPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(item => item.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

using Microsoft.AspNetCore.Identity;

namespace EmployeeProcurementPlanning.Infrastructure.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

using EmployeeProcurementPlanning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeProcurementPlanning.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/departments")]
public sealed class DepartmentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .OrderBy(department => department.Name)
            .Select(department => new
            {
                department.Id,
                department.Name,
                department.Code
            })
            .ToListAsync(cancellationToken);

        return Ok(departments);
    }
}

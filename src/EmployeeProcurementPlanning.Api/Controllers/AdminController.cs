using EmployeeProcurementPlanning.Application.DTOs;
using EmployeeProcurementPlanning.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeProcurementPlanning.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/v1/admin")]
public sealed class AdminController(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles,
    AppDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Users(CancellationToken cancellationToken)
    {
        var userList = await users.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        var departments = await db.Departments
            .AsNoTracking()
            .ToDictionaryAsync(department => department.Id, department => department.Name, cancellationToken);

        var result = new List<UserDto>(userList.Count);

        foreach (var user in userList)
        {
            var userRoles = (await users.GetRolesAsync(user)).ToArray();
            var departmentName = user.DepartmentId.HasValue &&
                                 departments.TryGetValue(user.DepartmentId.Value, out var name)
                ? name
                : null;

            result.Add(new UserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.DisplayName,
                departmentName,
                userRoles,
                user.IsActive,
                user.CreatedAtUtc));
        }

        return Ok(result);
    }

    [HttpPut("users/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var requestedRoles = request.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedRoles.Length != 1)
        {
            return BadRequest(new { message = "Exactly one application role must be assigned." });
        }

        foreach (var role in requestedRoles)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                return BadRequest(new { message = $"Unknown role: {role}." });
            }
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(currentUserId, out var actorId) && actorId == id && !request.IsActive)
        {
            return BadRequest(new { message = "You cannot deactivate your own administrator account." });
        }

        var currentRoles = await users.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToAdd = requestedRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await users.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await users.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }
        }

        user.IsActive = request.IsActive;
        user.SecurityStamp = Guid.NewGuid().ToString();
        var updateResult = await users.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            return BadRequest(updateResult.Errors);
        }

        return NoContent();
    }
}

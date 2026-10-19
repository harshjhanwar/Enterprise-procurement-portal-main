using EmployeeProcurementPlanning.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace EmployeeProcurementPlanning.ApiTests;

public sealed class ApiContractTests
{
    [Fact]
    public void PlansControllerUsesVersionOneRoute()
    {
        var route = typeof(PlansController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Single();

        Assert.Equal("api/v1/plans", route.Template);
    }

    [Fact]
    public void AdministrationControllerRequiresAdministratorRole()
    {
        var authorization = typeof(AdminController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("Administrator", authorization.Roles);
    }

    [Fact]
    public void PlansControllerRequiresAuthentication()
    {
        var authorization = typeof(PlansController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.True(string.IsNullOrWhiteSpace(authorization.Roles));
    }
}

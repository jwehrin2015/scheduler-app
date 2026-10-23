using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services.EmployeeService;

namespace SchedulerApp.Api.Endpoints;

public static class EmployeesEndpoint
{
    public static IEndpointRouteBuilder MapEmployeesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/employees", (IEmployeeService employeeService) =>
            employeeService.GetAllEmployeesAsync());

        app.MapPost("/api/employees", (EmployeeRequest request, IEmployeeService employeeService) =>
            employeeService.CreateEmployeeAsync(request));

        app.MapPut("/api/employees/{id:int}", (int id, EmployeeRequest request, IEmployeeService employeeService) =>
            employeeService.UpdateEmployeeAsync(id, request));

        app.MapDelete("/api/employees/{id:int}", (int id, IEmployeeService employeeService) =>
            employeeService.DeleteEmployeeAsync(id));

        return app;
    }
}

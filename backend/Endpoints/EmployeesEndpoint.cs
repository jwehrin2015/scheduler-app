using System;
using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Models;

namespace SchedulerApp.Api.Endpoints;

public static class EmployeesEndpoint
{
    public static IEndpointRouteBuilder MapEmployeesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/employees", async (SchedulerDbContext database) =>
    await database.Employees.AsNoTracking().OrderBy(employee => employee.LastName)
        .ThenBy(employee => employee.FirstName).ToListAsync());

        app.MapPost("/api/employees", async Task<IResult> (EmployeeRequest request, SchedulerDbContext database) =>
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return Results.BadRequest(new { error = "First and last name are required." });
            }

            var employee = new Employee
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                JobTitle = NullIfWhiteSpace(request.JobTitle),
                Email = NullIfWhiteSpace(request.Email),
                ContactPhoneNumber = NullIfWhiteSpace(request.ContactPhoneNumber)
            };

            database.Employees.Add(employee);
            await database.SaveChangesAsync();
            return Results.Created($"/api/employees/{employee.Id}", employee);
        });

        app.MapPut("/api/employees/{id:int}", async Task<IResult> (
            int id, EmployeeRequest request, SchedulerDbContext database) =>
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return Results.BadRequest(new { error = "First and last name are required." });
            }

            var employee = await database.Employees.FindAsync(id);
            if (employee is null)
            {
                return Results.NotFound();
            }

            employee.FirstName = request.FirstName.Trim();
            employee.LastName = request.LastName.Trim();
            employee.JobTitle = NullIfWhiteSpace(request.JobTitle);
            employee.Email = NullIfWhiteSpace(request.Email);
            employee.ContactPhoneNumber = NullIfWhiteSpace(request.ContactPhoneNumber);
            await database.SaveChangesAsync();
            return Results.Ok(employee);
        });

        app.MapDelete("/api/employees/{id:int}", async Task<IResult> (int id, SchedulerDbContext database) =>
        {
            var employee = await database.Employees.FindAsync(id);
            if (employee is null)
            {
                return Results.NotFound();
            }

            if (await database.Shifts.AnyAsync(shift => shift.EmployeeId == id))
            {
                return Results.Conflict(new { error = "Remove this employee's shifts before deleting them." });
            }

            database.Employees.Remove(employee);
            await database.SaveChangesAsync();
            return Results.NoContent();
        });
        return app;
    }

    static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}

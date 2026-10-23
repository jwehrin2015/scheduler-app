using System;
using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services.Logging;

namespace SchedulerApp.Api.Services.EmployeeService;

public class EmployeeService : IEmployeeService
{
    private readonly SchedulerDbContext _database;
    private readonly ILoggingService _loggingService;
    
    public EmployeeService(SchedulerDbContext database, ILoggingService loggingService)
    {
        _database = database;
        _loggingService = loggingService;
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        var employees = await _database.Employees
            .AsNoTracking()
            .OrderBy(employee => employee.LastName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync();
        await _loggingService.LogAsync($"Retrieved {employees.Count} employees from the database.");
        return employees;
    }

    public async Task<IResult> CreateEmployeeAsync(EmployeeRequest request)
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

        _database.Employees.Add(employee);
        await _database.SaveChangesAsync();
        await _loggingService.LogAsync($"Created employee {employee.Id}.");
        return Results.Created($"/api/employees/{employee.Id}", employee);
    }

    public async Task<IResult> UpdateEmployeeAsync(int id, EmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Results.BadRequest(new { error = "First and last name are required." });
        }

        var employee = await _database.Employees.FindAsync(id);
        if (employee is null)
        {
            return Results.NotFound();
        }

        employee.FirstName = request.FirstName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.JobTitle = NullIfWhiteSpace(request.JobTitle);
        employee.Email = NullIfWhiteSpace(request.Email);
        employee.ContactPhoneNumber = NullIfWhiteSpace(request.ContactPhoneNumber);
        await _database.SaveChangesAsync();
        await _loggingService.LogAsync($"Updated employee {employee.Id}.");
        return Results.Ok(employee);
    }

    public async Task<IResult> DeleteEmployeeAsync(int id)
    {
        var employee = await _database.Employees.FindAsync(id);
        if (employee is null)
        {
            return Results.NotFound();
        }

        if (await _database.Shifts.AnyAsync(shift => shift.EmployeeId == id))
        {
            return Results.Conflict(new { error = "Remove this employee's shifts before deleting them." });
        }

        _database.Employees.Remove(employee);
        await _database.SaveChangesAsync();
        await _loggingService.LogAsync($"Deleted employee {employee.Id}.");
        return Results.NoContent();
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

namespace SchedulerApp.Api.Models;

public sealed class Employee
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? ContactPhoneNumber { get; set; }
}

public sealed class EmployeeRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Email { get; set; }
    public string? ContactPhoneNumber { get; set; }
}

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Models;

var builder = WebApplication.CreateBuilder(args);
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "scheduler.db");

builder.Services.AddDbContext<SchedulerDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

await InitializeDatabaseAsync(app.Services);

app.UseCors("AngularDevelopment");

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

app.MapGet("/api/shifts", async Task<IResult> (
    DateOnly startDate, DateOnly endDate, SchedulerDbContext database) =>
{
    if (endDate < startDate || endDate.DayNumber - startDate.DayNumber > 42)
    {
        return Results.BadRequest(new { error = "Choose a date range of 1 to 43 days." });
    }

    var shifts = await database.Shifts.AsNoTracking()
        .Where(shift => shift.Date >= startDate && shift.Date <= endDate)
        .OrderBy(shift => shift.Date).ThenBy(shift => shift.StartTime)
        .ToListAsync();
    return Results.Ok(shifts);
});

app.MapPost("/api/shifts", async Task<IResult> (ShiftRequest request, SchedulerDbContext database) =>
{
    var validation = await ValidateShiftAsync(request, database);
    if (validation is not null)
    {
        return validation;
    }

    var shift = new Shift
    {
        EmployeeId = request.EmployeeId,
        Date = request.Date,
        StartTime = request.StartTime,
        EndTime = request.EndTime
    };

    database.Shifts.Add(shift);
    await database.SaveChangesAsync();
    return Results.Created($"/api/shifts/{shift.Id}", shift);
});

app.MapPut("/api/shifts/{id:int}", async Task<IResult> (
    int id, ShiftRequest request, SchedulerDbContext database) =>
{
    var shift = await database.Shifts.FindAsync(id);
    if (shift is null)
    {
        return Results.NotFound();
    }

    var validation = await ValidateShiftAsync(request, database, id);
    if (validation is not null)
    {
        return validation;
    }

    shift.EmployeeId = request.EmployeeId;
    shift.Date = request.Date;
    shift.StartTime = request.StartTime;
    shift.EndTime = request.EndTime;
    await database.SaveChangesAsync();
    return Results.Ok(shift);
});

app.MapDelete("/api/shifts/{id:int}", async Task<IResult> (int id, SchedulerDbContext database) =>
{
    var shift = await database.Shifts.FindAsync(id);
    if (shift is null)
    {
        return Results.NotFound();
    }

    database.Shifts.Remove(shift);
    await database.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

static async Task<IResult?> ValidateShiftAsync(
    ShiftRequest request, SchedulerDbContext database, int? excludedShiftId = null)
{
    if (request.EmployeeId <= 0 || request.EndTime <= request.StartTime)
    {
        return Results.BadRequest(new { error = "Choose an employee and a valid start and end time." });
    }

    if (!await database.Employees.AnyAsync(employee => employee.Id == request.EmployeeId))
    {
        return Results.BadRequest(new { error = "The selected employee does not exist." });
    }

    var overlaps = await database.Shifts.AnyAsync(shift =>
        shift.EmployeeId == request.EmployeeId &&
        shift.Date == request.Date &&
        shift.StartTime < request.EndTime &&
        shift.EndTime > request.StartTime &&
        (!excludedShiftId.HasValue || shift.Id != excludedShiftId.Value));

    return overlaps
        ? Results.Conflict(new { error = "This shift overlaps another shift assigned to the employee." })
        : null;
}

static string? NullIfWhiteSpace(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

static async Task InitializeDatabaseAsync(IServiceProvider services)
{
    await using var scope = services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await database.Database.EnsureCreatedAsync();

    var connection = database.Database.GetDbConnection();
    await connection.OpenAsync();
    var legacyStudentsExist = await TableExistsAsync(connection, "Students");

    await database.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS Employees (
            Id INTEGER NOT NULL CONSTRAINT PK_Employees PRIMARY KEY AUTOINCREMENT,
            FirstName TEXT NOT NULL,
            LastName TEXT NOT NULL,
            JobTitle TEXT NULL,
            Email TEXT NULL,
            ContactPhoneNumber TEXT NULL
        );
        CREATE TABLE IF NOT EXISTS Shifts (
            Id INTEGER NOT NULL CONSTRAINT PK_Shifts PRIMARY KEY AUTOINCREMENT,
            EmployeeId INTEGER NOT NULL,
            Date TEXT NOT NULL,
            StartTime TEXT NOT NULL,
            EndTime TEXT NOT NULL,
            CONSTRAINT FK_Shifts_Employees_EmployeeId FOREIGN KEY (EmployeeId)
                REFERENCES Employees (Id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS IX_Shifts_EmployeeId ON Shifts (EmployeeId);
        CREATE INDEX IF NOT EXISTS IX_Shifts_Date ON Shifts (Date);
        """);

    if (legacyStudentsExist)
    {
        await database.Database.ExecuteSqlRawAsync("""
            INSERT OR IGNORE INTO Employees (Id, FirstName, LastName, JobTitle, Email, ContactPhoneNumber)
            SELECT Id, FirstName, COALESCE(LastName, ''), NULL, Email, ContactPhoneNumber
            FROM Students;
            DROP TABLE Students;
            """);
    }
}

static async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName;";
    var parameter = command.CreateParameter();
    parameter.ParameterName = "@tableName";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);
    return Convert.ToInt64(await command.ExecuteScalarAsync()) > 0;
}

public partial class Program;

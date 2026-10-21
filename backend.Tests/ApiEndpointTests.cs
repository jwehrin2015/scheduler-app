using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Endpoints;
using SchedulerApp.Api.Models;

namespace SchedulerApp.Api.Tests;

public sealed class ApiEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public ApiEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetEmployees_ReturnsEmployeesOrderedByLastNameThenFirstName()
    {
        await ResetDatabaseAsync();
        await SeedAsync(
            new Employee { FirstName = "Ada", LastName = "Lovelace", JobTitle = "Mathematician" },
            new Employee { FirstName = "Grace", LastName = "Hopper", JobTitle = "Scientist" },
            new Employee { FirstName = "Alan", LastName = "Lovelace", JobTitle = "Engineer" });

        var response = await _client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employees = await response.Content.ReadFromJsonAsync<List<Employee>>();
        Assert.NotNull(employees);
        Assert.Equal(new[] { "Hopper, Grace", "Lovelace, Ada", "Lovelace, Alan" },
            employees.Select(employee => employee.LastName + ", " + employee.FirstName).ToArray());
    }

    [Fact]
    public async Task PostEmployee_WithMissingNames_ReturnsBadRequest()
    {
        await ResetDatabaseAsync();

        var response = await _client.PostAsJsonAsync("/api/employees", new EmployeeRequest
        {
            FirstName = "",
            LastName = "",
            JobTitle = "Developer"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("First and last name are required.", payload["error"]);
    }

    [Fact]
    public async Task PostShift_CreatesShift_WhenValidAndNonOverlapping()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });

        var response = await _client.PostAsJsonAsync("/api/shifts", new ShiftRequest
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var shift = await response.Content.ReadFromJsonAsync<Shift>();
        Assert.NotNull(shift);
        Assert.Equal(employee.Id, shift.EmployeeId);
        Assert.Equal(new DateOnly(2026, 10, 8), shift.Date);
    }

    [Fact]
    public async Task PostShift_WithOverlap_ReturnsConflict()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(new Shift
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        });

        var response = await _client.PostAsJsonAsync("/api/shifts", new ShiftRequest
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(11, 30),
            EndTime = new TimeOnly(13, 0)
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("This shift overlaps another shift assigned to the employee.", payload["error"]);
    }

    [Fact]
    public async Task GetShifts_ReturnsOnlyShiftsWithinRequestedDateRange()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(
            new Shift
            {
                EmployeeId = employee.Id,
                Date = new DateOnly(2026, 10, 4),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0)
            },
            new Shift
            {
                EmployeeId = employee.Id,
                Date = new DateOnly(2026, 10, 8),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0)
            },
            new Shift
            {
                EmployeeId = employee.Id,
                Date = new DateOnly(2026, 10, 12),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(12, 0)
            });

        var response = await _client.GetAsync("/api/shifts?startDate=2026-10-05&endDate=2026-10-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shifts = await response.Content.ReadFromJsonAsync<List<Shift>>();
        Assert.NotNull(shifts);
        var shift = Assert.Single(shifts);
        Assert.Equal(new DateOnly(2026, 10, 8), shift.Date);
    }

    [Theory]
    [InlineData("2026-10-12", "2026-10-11")]
    [InlineData("2026-10-01", "2026-11-13")]
    public async Task GetShifts_WithInvalidDateRange_ReturnsBadRequest(string startDate, string endDate)
    {
        await ResetDatabaseAsync();

        var response = await _client.GetAsync($"/api/shifts?startDate={startDate}&endDate={endDate}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutShift_UpdatesShift_WhenValidAndNonOverlapping()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(new Shift
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        });
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var shiftId = await database.Shifts.Select(shift => shift.Id).SingleAsync();

        var response = await _client.PutAsJsonAsync($"/api/shifts/{shiftId}", new ShiftRequest
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 9),
            StartTime = new TimeOnly(13, 0),
            EndTime = new TimeOnly(17, 0)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedShift = await response.Content.ReadFromJsonAsync<Shift>();
        Assert.NotNull(updatedShift);
        Assert.Equal(new DateOnly(2026, 10, 9), updatedShift.Date);
        Assert.Equal(new TimeOnly(13, 0), updatedShift.StartTime);
    }

    [Fact]
    public async Task DeleteShift_RemovesExistingShift()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(new Shift
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        });
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        var shiftId = await database.Shifts.Select(shift => shift.Id).SingleAsync();

        var response = await _client.DeleteAsync($"/api/shifts/{shiftId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await database.Shifts.AnyAsync(shift => shift.Id == shiftId));
    }

    [Fact]
    public async Task DeleteEmployee_WithAssignedShifts_ReturnsConflict()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(new Shift
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        });

        var response = await _client.DeleteAsync($"/api/employees/{employee.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.Equal("Remove this employee's shifts before deleting them.", payload["error"]);
    }

    [Fact]
    public async Task ValidateShiftAsync_ReturnsBadRequest_WhenEmployeeIsMissingOrTimesAreInvalid()
    {
        await ResetDatabaseAsync();
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var missingEmployee = await ShiftsEndpoint.ValidateShiftAsync(
            new ShiftRequest { EmployeeId = 999, Date = new DateOnly(2026, 10, 8), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) },
            database);
        Assert.NotNull(missingEmployee);
        Assert.Equal((int)HttpStatusCode.BadRequest, GetStatusCode(missingEmployee));

        var invalidTime = await ShiftsEndpoint.ValidateShiftAsync(
            new ShiftRequest { EmployeeId = 1, Date = new DateOnly(2026, 10, 8), StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(9, 0) },
            database);
        Assert.NotNull(invalidTime);
        Assert.Equal((int)HttpStatusCode.BadRequest, GetStatusCode(invalidTime));
    }

    [Fact]
    public async Task ValidateShiftAsync_ReturnsConflict_WhenShiftOverlapsExistingAssignment()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(new Shift
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        });

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var result = await ShiftsEndpoint.ValidateShiftAsync(
            new ShiftRequest { EmployeeId = employee.Id, Date = new DateOnly(2026, 10, 8), StartTime = new TimeOnly(11, 30), EndTime = new TimeOnly(13, 0) },
            database);

        Assert.NotNull(result);
        Assert.Equal((int)HttpStatusCode.Conflict, GetStatusCode(result));
    }

    [Fact]
    public async Task ValidateShiftAsync_ReturnsNull_WhenShiftDoesNotOverlapAnotherAssignment()
    {
        await ResetDatabaseAsync();
        var employee = await SeedEmployeeAsync(new Employee { FirstName = "Ada", LastName = "Lovelace" });
        await SeedAsync(new Shift
        {
            EmployeeId = employee.Id,
            Date = new DateOnly(2026, 10, 8),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0)
        });

        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        var result = await ShiftsEndpoint.ValidateShiftAsync(
            new ShiftRequest { EmployeeId = employee.Id, Date = new DateOnly(2026, 10, 8), StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 0) },
            database);

        Assert.Null(result);
    }

    private static int GetStatusCode(IResult result)
    {
        return result.GetType().GetProperty("StatusCode")?.GetValue(result) as int? ?? 0;
    }

    private async Task<Employee> SeedEmployeeAsync(Employee employee)
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        database.Employees.Add(employee);
        await database.SaveChangesAsync();
        return employee;
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
        database.Shifts.RemoveRange(database.Shifts);
        database.Employees.RemoveRange(database.Employees);
        await database.SaveChangesAsync();
    }

    private async Task SeedAsync(params object[] entities)
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();

        foreach (var entity in entities)
        {
            switch (entity)
            {
                case Employee employee:
                    database.Employees.Add(employee);
                    break;
                case Shift shift:
                    database.Shifts.Add(shift);
                    break;
            }
        }

        await database.SaveChangesAsync();
    }
}

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SchedulerDbContext>>();
            services.RemoveAll<SchedulerDbContext>();

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            services.AddSingleton<DbConnection>(connection);
            services.AddDbContext<SchedulerDbContext>(options => options.UseSqlite(connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
            database.Database.Migrate();
        });
    }
}

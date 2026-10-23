using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services.Logging;

namespace SchedulerApp.Api.Services.ShiftService;

public sealed class ShiftService(SchedulerDbContext database, ILoggingService loggingService) : IShiftService
{
    public async Task<IResult> GetShiftsAsync(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate || endDate.DayNumber - startDate.DayNumber > 42)
        {
            return Results.BadRequest(new { error = "Choose a date range of 1 to 43 days." });
        }

        var shifts = await database.Shifts.AsNoTracking()
            .Where(shift => shift.Date >= startDate && shift.Date <= endDate)
            .OrderBy(shift => shift.Date)
            .ThenBy(shift => shift.StartTime)
            .ToListAsync();
        await loggingService.LogAsync($"Retrieved {shifts.Count} shifts from the database.");
        return Results.Ok(shifts);
    }

    public async Task<IResult> CreateShiftAsync(ShiftRequest request)
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
        await loggingService.LogAsync($"Created shift {shift.Id}.");
        return Results.Created($"/api/shifts/{shift.Id}", shift);
    }

    public async Task<IResult> UpdateShiftAsync(int id, ShiftRequest request)
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
        await loggingService.LogAsync($"Updated shift {shift.Id}.");
        return Results.Ok(shift);
    }

    public async Task<IResult> DeleteShiftAsync(int id)
    {
        var shift = await database.Shifts.FindAsync(id);
        if (shift is null)
        {
            return Results.NotFound();
        }

        database.Shifts.Remove(shift);
        await database.SaveChangesAsync();
        await loggingService.LogAsync($"Deleted shift {shift.Id}.");
        return Results.NoContent();
    }

    internal static async Task<IResult?> ValidateShiftAsync(
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
}

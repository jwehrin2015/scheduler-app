using Microsoft.EntityFrameworkCore;
using SchedulerApp.Api.Data;
using SchedulerApp.Api.Models;

namespace SchedulerApp.Api.Endpoints
{
    public static class ShiftsEndpoint
    {
        public static IEndpointRouteBuilder MapShiftsEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/shifts", async (SchedulerDbContext database) =>
                await database.Shifts
                .AsNoTracking()
                .OrderBy(shift => shift.Date)
                    .ThenBy(shift => shift.StartTime)
                .ToListAsync());

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

            return app;
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
}
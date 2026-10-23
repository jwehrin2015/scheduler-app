using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services.ShiftService;

namespace SchedulerApp.Api.Endpoints;

public static class ShiftsEndpoint
{
    public static IEndpointRouteBuilder MapShiftsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/shifts", (
            DateOnly startDate, DateOnly endDate, IShiftService shiftService) =>
            shiftService.GetShiftsAsync(startDate, endDate));

        app.MapPost("/api/shifts", (ShiftRequest request, IShiftService shiftService) =>
            shiftService.CreateShiftAsync(request));

        app.MapPut("/api/shifts/{id:int}", (
            int id, ShiftRequest request, IShiftService shiftService) =>
            shiftService.UpdateShiftAsync(id, request));

        app.MapDelete("/api/shifts/{id:int}", (int id, IShiftService shiftService) =>
            shiftService.DeleteShiftAsync(id));

        return app;
    }
}

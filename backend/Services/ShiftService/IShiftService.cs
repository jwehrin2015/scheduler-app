using SchedulerApp.Api.Models;

namespace SchedulerApp.Api.Services.ShiftService;

public interface IShiftService
{
    Task<IResult> GetShiftsAsync(DateOnly startDate, DateOnly endDate);
    Task<IResult> CreateShiftAsync(ShiftRequest request);
    Task<IResult> UpdateShiftAsync(int id, ShiftRequest request);
    Task<IResult> DeleteShiftAsync(int id);
}

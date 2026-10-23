namespace SchedulerApp.Api.Services.Logging;

public interface ILoggingService
{
    Task LogAsync(string message);
}

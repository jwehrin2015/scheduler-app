using SchedulerApp.Api.Services.Logging;

namespace SchedulerApp.Api.Tests;

public sealed class FileLoggingServiceTests
{
    [Fact]
    public async Task LogAsync_AppendsTimestampedEntriesToFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"scheduler-app-{Guid.NewGuid():N}.txt");
        using var loggingService = new FileLoggingService(filePath);

        try
        {
            await loggingService.LogAsync("first entry");
            await loggingService.LogAsync("second entry");

            var entries = await File.ReadAllLinesAsync(filePath);
            Assert.Equal(2, entries.Length);
            Assert.StartsWith(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"), entries[0]);
            Assert.EndsWith("first entry", entries[0]);
            Assert.EndsWith("second entry", entries[1]);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}

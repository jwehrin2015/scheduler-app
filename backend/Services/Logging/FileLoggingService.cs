using System.Text;

namespace SchedulerApp.Api.Services.Logging;

public sealed class FileLoggingService(string filePath) : ILoggingService, IDisposable
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task LogAsync(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        await _writeLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var entry = $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}";
            await File.AppendAllTextAsync(filePath, entry, Encoding.UTF8);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose() => _writeLock.Dispose();
}

namespace Blog.ActivityHub.Api.Contracts;

public interface IBlogMonitorService
{
    Task SyncEntriesAsync(CancellationToken token = default);
}
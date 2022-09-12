using Blog.ActivityHub.Api.Contracts;

namespace Blog.ActivityHub.Api.Worker;

public class BlogMonitorWorker : BackgroundService
{
    private const int MonitorPeriodMilliseconds = 1000 * 60 * 10; // Ten minutes

    private readonly ILogger<BlogMonitorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BlogMonitorWorker(ILogger<BlogMonitorWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting blog monitor background service");
        while (!stoppingToken.IsCancellationRequested)
        {
            await using (var scope = _serviceProvider.CreateAsyncScope())
            {
                var monitorService = scope.ServiceProvider.GetRequiredService<IBlogMonitorService>();
                try
                {
                    await monitorService.SyncEntriesAsync(stoppingToken);
                    _logger.LogDebug("Finished syncing blog entries");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to sync blog entries");
                }
            }

            _logger.LogDebug("Waiting {Period} before next run", TimeSpan.FromMilliseconds(MonitorPeriodMilliseconds));
            await Task.Delay(MonitorPeriodMilliseconds, stoppingToken);
        }

        _logger.LogInformation("Stopped blog monitor background service");
    }
}
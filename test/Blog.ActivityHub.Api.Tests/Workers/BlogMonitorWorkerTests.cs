using Blog.ActivityHub.Api.Contracts;
using Blog.ActivityHub.Api.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Blog.ActivityHub.Api.Tests.Workers;

public class BlogMonitorWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldNotCallService_WhereCancellationRequested()
    {
        var blogMonitorMock = new Mock<IBlogMonitorService>();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(blogMonitorMock.Object);

        var provider = serviceCollection.BuildServiceProvider();
        var worker = new BlogMonitorWorker(NullLogger<BlogMonitorWorker>.Instance, provider);

        var token = new CancellationTokenSource();
        token.Cancel();

        await worker.StartAsync(token.Token);

        blogMonitorMock.Verify(x =>
                x.SyncEntriesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
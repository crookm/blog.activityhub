using Blog.ActivityHub.Api.Options;
using Blog.ActivityHub.Api.Services;
using Blog.ActivityHub.Api.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Blog.ActivityHub.Api.Tests.Services;

public class BlogMonitorServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\f")]
    [InlineData("\r\n")]
    public async Task SyncEntriesAsync_ShouldThrow_WhereBaseAddressEmpty(string baseAddressInput)
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = baseAddressInput, FeedPath = "something" });

        var service = new BlogMonitorService(NullLogger<BlogMonitorService>.Instance, context, blogOptions);
        var act = () => service.SyncEntriesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("*baseaddress*");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\f")]
    [InlineData("\r\n")]
    public async Task SyncEntriesAsync_ShouldThrow_WhereFeedPathEmpty(string feedPathInput)
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "something", FeedPath = feedPathInput });

        var service = new BlogMonitorService(NullLogger<BlogMonitorService>.Instance, context, blogOptions);
        var act = () => service.SyncEntriesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<Exception>().WithMessage("*feedpath*");
    }
}
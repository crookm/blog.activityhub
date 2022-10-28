using System.Net;
using Aoraki.Events.Contracts;
using Blog.ActivityHub.Api.Data.Models;
using Blog.ActivityHub.Api.Options;
using Blog.ActivityHub.Api.Services;
using Blog.ActivityHub.Api.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Blog.ActivityHub.Api.Tests.Services;

public class ReactionServiceTests
{
    [Fact]
    public async Task GetUserReaction_ShouldThrow_WhereEntryIsNull()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var act = () => service.GetUserReaction(null!, IPAddress.None, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*entry*");
    }

    [Fact]
    public async Task GetUserReaction_ShouldThrow_WhereIpIsNull()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var act = () => service.GetUserReaction(new Entry(), null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*ipaddress*");
    }

    [Fact]
    public async Task GetUserReaction_ShouldReturnDefault_WhereNoReaction()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var result = await service.GetUserReaction(new Entry { Id = 1 }, IPAddress.None, CancellationToken.None);
        result.Should().Be(ActivityHub.Contracts.Reaction.None);
    }

    [Fact]
    public async Task GetUserReaction_ShouldReturnDefault_WhereReactionRemoved()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        context.Reactions.Add(new Reaction
        {
            Entry = new Entry { Id = 1 }, IpAddress = IPAddress.None,
            Removed = DateTime.UtcNow.AddDays(-10),
            ReactionType = ActivityHub.Contracts.Reaction.Educational
        });
        await context.SaveChangesAsync();

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var result = await service.GetUserReaction(new Entry { Id = 1 }, IPAddress.None, CancellationToken.None);
        result.Should().Be(ActivityHub.Contracts.Reaction.None);
    }

    [Fact]
    public async Task GetUserReaction_ShouldBeAccurate()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        context.Reactions.Add(new Reaction
        {
            Entry = new Entry { Id = 1 }, IpAddress = IPAddress.None,
            ReactionType = ActivityHub.Contracts.Reaction.Educational
        });
        await context.SaveChangesAsync();

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var result = await service.GetUserReaction(new Entry { Id = 1 }, IPAddress.None, CancellationToken.None);
        result.Should().Be(ActivityHub.Contracts.Reaction.Educational);
    }

    [Fact]
    public async Task GetReactionTotals_ShouldThrow_WhereEntryIsNull()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var act = () => service.GetReactionTotals(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetReactionTotals_ShouldBeAccurate()
    {
        await using var context = DbContextHelpers.CreateDbContext();
        var blogOptions = Microsoft.Extensions.Options.Options.Create(new BlogOptions
            { BaseAddress = "stuff", FeedPath = "something" });

        var entryOne = new Entry { Id = 1 };
        var entryTwo = new Entry { Id = 2 };
        context.Reactions.AddRange(
            new Reaction { Entry = entryOne, ReactionType = ActivityHub.Contracts.Reaction.Like },
            new Reaction { Entry = entryOne, ReactionType = ActivityHub.Contracts.Reaction.Like },
            new Reaction { Entry = entryOne, ReactionType = ActivityHub.Contracts.Reaction.Educational },
            new Reaction { Entry = entryOne, ReactionType = ActivityHub.Contracts.Reaction.Outdated },
            new Reaction { Entry = entryTwo, ReactionType = ActivityHub.Contracts.Reaction.Like });
        await context.SaveChangesAsync();

        var service = new ReactionService(NullLogger<ReactionService>.Instance, context, blogOptions,
            new Mock<IBlogEventPublisher>().Object);
        var result = await service.GetReactionTotals(new Entry { Id = 1 }, CancellationToken.None);

        result[ActivityHub.Contracts.Reaction.Like].Should().Be(2);
        result[ActivityHub.Contracts.Reaction.Educational].Should().Be(1);
        result[ActivityHub.Contracts.Reaction.Outdated].Should().Be(1);
    }
}
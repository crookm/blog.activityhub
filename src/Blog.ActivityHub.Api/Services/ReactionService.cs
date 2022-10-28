using System.Net;
using Aoraki.Events.Contracts;
using Aoraki.Events.Contracts.Blog;
using Ardalis.GuardClauses;
using Blog.ActivityHub.Api.Contracts;
using Blog.ActivityHub.Api.Data;
using Blog.ActivityHub.Api.Options;
using Blog.ActivityHub.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Entry = Blog.ActivityHub.Api.Data.Models.Entry;
using Reaction = Blog.ActivityHub.Api.Data.Models.Reaction;

namespace Blog.ActivityHub.Api.Services;

public class ReactionService : IReactionService
{
    private readonly ILogger<ReactionService> _logger;
    private readonly ActivityDbContext _dbContext;
    private readonly BlogOptions _blogOptions;
    private readonly IBlogEventPublisher _eventPublisher;

    public ReactionService(ILogger<ReactionService> logger, ActivityDbContext dbContext,
        IOptions<BlogOptions> blogOptions, IBlogEventPublisher eventPublisher)
    {
        _logger = logger;
        _dbContext = dbContext;
        _blogOptions = blogOptions.Value;
        _eventPublisher = eventPublisher;
    }

    public async Task PostReactionAsync(Entry entry, IPAddress ipAddress, Blog.ActivityHub.Contracts.Reaction reaction,
        CancellationToken token = default)
    {
        Guard.Against.Null(entry);
        Guard.Against.Null(ipAddress);
        Guard.Against.Null(reaction);

        _logger.LogInformation("Posting a {Reaction} reaction to entry {Entry} from {IpAddress}",
            reaction, entry.Id, ipAddress);

        var prevReactions = await _dbContext.Reactions
            .OrderByDescending(r => r.Created)
            .Where(r => r.Entry == entry && r.Removed == null && r.IpAddress == ipAddress)
            .ToListAsync(token);

        // Remove any previous reactions first
        if (prevReactions.Count > 0)
        {
            if (prevReactions.First().ReactionType == reaction) return;
            foreach (var prevReaction in prevReactions)
            {
                await PublishReactionRemovedEvent(entry, prevReaction.ReactionType, ipAddress);
                prevReaction.Removed = DateTimeOffset.Now.UtcDateTime;
            }

            _dbContext.Reactions.UpdateRange(prevReactions);
        }

        // Apply the new reaction, but only if it is not 'none'
        if (reaction != Blog.ActivityHub.Contracts.Reaction.None)
        {
            await PublishReactionCreatedEvent(entry, reaction, ipAddress);
            _dbContext.Reactions.Add(new Reaction
            {
                Entry = entry,
                ReactionType = reaction,
                IpAddress = ipAddress
            });
        }

        await _dbContext.SaveChangesAsync(token);
    }

    public async Task<ActivityHub.Contracts.Reaction> GetUserReaction(Entry entry, IPAddress ipAddress,
        CancellationToken token = default)
    {
        Guard.Against.Null(entry);
        Guard.Against.Null(ipAddress);

        return await _dbContext.Reactions
            .AsNoTracking()
            .OrderByDescending(r => r.Created)
            .Where(r => r.Entry == entry && r.Removed == null && r.IpAddress == ipAddress)
            .Select(r => r.ReactionType)
            .FirstOrDefaultAsync(token);
    }

    public async Task<Dictionary<ActivityHub.Contracts.Reaction, int>> GetReactionTotals(Entry entry,
        CancellationToken token = default)
    {
        Guard.Against.Null(entry);

        return await _dbContext.Reactions
            .AsNoTracking()
            .Where(r => r.Entry == entry && r.Removed == null)
            .GroupBy(r => r.ReactionType)
            .Select(g => new { Reaction = g.Key, Count = g.Count() })
            .ToDictionaryAsync(k => k.Reaction, v => v.Count, token);
    }

    #region Helpers

    private async Task PublishReactionCreatedEvent(Entry entry, Blog.ActivityHub.Contracts.Reaction reaction,
        IPAddress ipAddress) => await _eventPublisher.SendReactionCreatedEventAsync(
        $"{_blogOptions.BaseAddress}{entry.RelPermalink}", new ReactionCreatedEvent
        {
            BlogName = Constants.BlogName,
            BlogHost = new Uri(_blogOptions.BaseAddress).Host,
            ReactionTypeId = (int)reaction,
            ReactionTypeName = reaction.ToString(),
            IpAddress = ipAddress.ToString()
        });

    private async Task PublishReactionRemovedEvent(Entry entry, Blog.ActivityHub.Contracts.Reaction reaction,
        IPAddress ipAddress) => await _eventPublisher.SendReactionRemovedEventAsync(
        $"{_blogOptions.BaseAddress}{entry.RelPermalink}", new ReactionRemovedEvent
        {
            BlogName = Constants.BlogName,
            BlogHost = new Uri(_blogOptions.BaseAddress).Host,
            ReactionTypeId = (int)reaction,
            ReactionTypeName = reaction.ToString(),
            IpAddress = ipAddress.ToString()
        });

    #endregion
}
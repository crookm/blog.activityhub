using System.Net;
using Ardalis.GuardClauses;
using Blog.ActivityHub.Api.Contracts;
using Blog.ActivityHub.Api.Data;
using Blog.ActivityHub.Api.Data.Models;
using Microsoft.EntityFrameworkCore;
using Reaction = Blog.ActivityHub.Api.Data.Models.Reaction;

namespace Blog.ActivityHub.Api.Services;

public class ReactionService : IReactionService
{
    private readonly ILogger<ReactionService> _logger;
    private readonly ActivityDbContext _dbContext;

    public ReactionService(ILogger<ReactionService> logger, ActivityDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
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
            foreach (var prevReaction in prevReactions) prevReaction.Removed = DateTimeOffset.Now.UtcDateTime;
            _dbContext.Reactions.UpdateRange(prevReactions);
        }

        // Apply the new reaction, but only if it is not 'none'
        if (reaction != Blog.ActivityHub.Contracts.Reaction.None)
            _dbContext.Reactions.Add(new Reaction
            {
                Entry = entry,
                ReactionType = reaction,
                IpAddress = ipAddress
            });
        
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
}
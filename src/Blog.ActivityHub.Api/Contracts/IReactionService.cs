using System.Net;
using Blog.ActivityHub.Api.Data.Models;

namespace Blog.ActivityHub.Api.Contracts;

public interface IReactionService
{
    Task PostReactionAsync(Entry entry, IPAddress ipAddress, Blog.ActivityHub.Contracts.Reaction reaction,
        CancellationToken token = default);

    Task<Blog.ActivityHub.Contracts.Reaction> GetUserReaction(Entry entry, IPAddress ipAddress,
        CancellationToken token = default);

    Task<Dictionary<Blog.ActivityHub.Contracts.Reaction, int>> GetReactionTotals(Entry entry,
        CancellationToken token = default);
}
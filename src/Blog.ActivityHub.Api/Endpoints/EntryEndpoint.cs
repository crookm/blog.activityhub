using Blog.ActivityHub.Api.Data;
using Blog.ActivityHub.Contracts;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Blog.ActivityHub.Api.Endpoints;

public class EntryEndpoint : Entry.EntryBase
{
    private readonly ILogger<EntryEndpoint> _logger;
    private readonly ActivityDbContext _dbContext;

    public EntryEndpoint(ILogger<EntryEndpoint> logger, ActivityDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public override async Task<GetEntryIdResponse> GetEntryId(GetEntryIdRequest request, ServerCallContext context)
    {
        var entryId = await _dbContext.Entries.AsNoTracking()
            .OrderByDescending(e => e.Created)
            .Where(e => e.RelPermalink == request.RelPermalink)
            .Select(e => e.Id)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (entryId == default)
            throw new RpcException(new Status(StatusCode.NotFound, "could not find specified entry"));

        return new GetEntryIdResponse { EntryId = entryId };
    }
}
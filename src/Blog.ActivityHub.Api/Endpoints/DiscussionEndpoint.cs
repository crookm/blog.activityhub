using Blog.ActivityHub.Api.Contracts;
using Blog.ActivityHub.Api.Data;
using Blog.ActivityHub.Api.Data.Models;
using Blog.ActivityHub.Api.Extensions;
using Blog.ActivityHub.Contracts;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Blog.ActivityHub.Api.Endpoints;

public class DiscussionEndpoint : Discussion.DiscussionBase
{
    private readonly ILogger<DiscussionEndpoint> _logger;
    private readonly ActivityDbContext _dbContext;
    private readonly IReactionService _reactionService;

    public DiscussionEndpoint(ILogger<DiscussionEndpoint> logger, ActivityDbContext dbContext,
        IReactionService reactionService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _reactionService = reactionService;
    }

    public override async Task<GetReactionResponse> GetReaction(GetReactionRequest request, ServerCallContext context)
    {
        var entry = await _dbContext.Entries.FirstOrDefaultAsync(e => e.Id == request.EntryId,
            context.CancellationToken);
        if (entry == null)
            throw new RpcException(new Status(StatusCode.NotFound, "could not find entry with id"));

        var ipAddress = context.GetHttpContext().GetTrueIp();
        return new GetReactionResponse
            { Reaction = await _reactionService.GetUserReaction(entry, ipAddress, context.CancellationToken) };
    }

    public override async Task<GetReactionsResponse> GetReactions(GetReactionRequest request, ServerCallContext context)
    {
        var entry = await _dbContext.Entries.FirstOrDefaultAsync(e => e.Id == request.EntryId,
            context.CancellationToken);
        if (entry == null)
            throw new RpcException(new Status(StatusCode.NotFound, "could not find entry with id"));

        var result = await _reactionService.GetReactionTotals(entry, context.CancellationToken);
        return new GetReactionsResponse
        {
            Reaction =
            {
                result.Select(r => new GetReactionsResponse.Types.ReactionSubtotal
                {
                    Reaction = r.Key,
                    Count = r.Value
                })
            }
        };
    }

    public override async Task<Empty> PostReaction(PostReactionRequest request, ServerCallContext context)
    {
        var entry = await _dbContext.Entries.FirstOrDefaultAsync(e => e.Id == request.EntryId,
            context.CancellationToken);
        if (entry == null)
            throw new RpcException(new Status(StatusCode.NotFound, "could not find entry with id"));

        await using var tx = await _dbContext.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            var ipAddress = context.GetHttpContext().GetTrueIp();
            await _reactionService.PostReactionAsync(entry, ipAddress, request.Reaction, context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);
            return new Empty();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while reacting to an entry");
            await tx.RollbackAsync(context.CancellationToken);

            throw;
        }
    }

    public override async Task<GetCommenterResponse> GetCommenter(Empty request, ServerCallContext context)
    {
        var ipAddress = context.GetHttpContext().GetTrueIp();
        var recentName = await _dbContext.Comments
            .AsNoTracking()
            .OrderByDescending(c => c.Created)
            .Where(c => c.IpAddress == ipAddress && c.Removed == null)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (string.IsNullOrWhiteSpace(recentName))
            throw new RpcException(new Status(StatusCode.NotFound, "details about the commenter could not be found"));

        return new GetCommenterResponse { Name = recentName };
    }

    public override async Task GetComments(GetCommentsRequest request,
        IServerStreamWriter<CommentResponse> responseStream, ServerCallContext context)
    {
        var entry = await _dbContext.Entries.FirstOrDefaultAsync(e => e.Id == request.EntryId,
            context.CancellationToken);
        if (entry == null)
            throw new RpcException(new Status(StatusCode.NotFound, "could not find entry with id"));

        var ipAddress = context.GetHttpContext().GetTrueIp();
        var comments = _dbContext.Comments
            .AsNoTracking()
            .OrderByDescending(c => c.Created)
            .Where(c => c.Entry == entry && c.Removed == null)
            .Select(c => new CommentResponse
            {
                CommentId = c.Id,
                Created = new DateTime(c.Created.Ticks, DateTimeKind.Utc).ToTimestamp(),
                Name = c.Name,
                Content = c.Content,
                IsYours = c.IpAddress == ipAddress
            })
            .AsAsyncEnumerable()
            .WithCancellation(context.CancellationToken);

        await foreach (var comment in comments)
            await responseStream.WriteAsync(comment, context.CancellationToken);
    }

    public override async Task<Empty> PostComment(PostCommentRequest request, ServerCallContext context)
    {
        request.Name = request.Name.Trim().ToLowerInvariant();
        request.Content = request.Content.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Content)
            || request.Name.Length > 25 || request.Content.Length > 256)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "invalid name or content"));

        var entry = await _dbContext.Entries.FirstOrDefaultAsync(e => e.Id == request.EntryId,
            context.CancellationToken);
        if (entry == null)
            throw new RpcException(new Status(StatusCode.NotFound, "could not find entry with id"));

        var ipAddress = context.GetHttpContext().GetTrueIp();
        var lastComment = await _dbContext.Comments
            .AsNoTracking()
            .OrderByDescending(c => c.Created)
            .FirstOrDefaultAsync(c => c.Entry == entry && c.IpAddress == ipAddress && c.Removed == null,
                cancellationToken: context.CancellationToken);

        if (lastComment != null && lastComment.Created > DateTime.UtcNow - TimeSpan.FromMinutes(30))
            throw new RpcException(new Status(StatusCode.ResourceExhausted,
                "commenter has already posted to this entry recently"));

        _dbContext.Comments.Add(new Comment
        {
            Entry = entry,
            IpAddress = ipAddress,
            Name = request.Name,
            Content = request.Content
        });
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        return new Empty();
    }
}
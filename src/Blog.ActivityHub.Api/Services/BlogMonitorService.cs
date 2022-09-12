using System.ServiceModel.Syndication;
using System.Xml;
using Ardalis.GuardClauses;
using Blog.ActivityHub.Api.Contracts;
using Blog.ActivityHub.Api.Data;
using Blog.ActivityHub.Api.Data.Models;
using Blog.ActivityHub.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Blog.ActivityHub.Api.Services;

public class BlogMonitorService : IBlogMonitorService
{
    private readonly ILogger<BlogMonitorService> _logger;
    private readonly ActivityDbContext _dbContext;
    private readonly BlogOptions _blogOptions;

    public BlogMonitorService(ILogger<BlogMonitorService> logger, ActivityDbContext dbContext,
        IOptions<BlogOptions> blogOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _blogOptions = blogOptions.Value;
    }

    public async Task SyncEntriesAsync(CancellationToken token = default)
    {
        Guard.Against.NullOrWhiteSpace(_blogOptions.BaseAddress);
        Guard.Against.NullOrWhiteSpace(_blogOptions.FeedPath);

        var reader = XmlReader.Create($"{_blogOptions.BaseAddress}{_blogOptions.FeedPath}",
            new XmlReaderSettings { Async = true });
        var feed = SyndicationFeed.Load(reader);

        foreach (var item in feed.Items)
        {
            var relPermalink = item.Links.FirstOrDefault()?.Uri.AbsolutePath.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(relPermalink))
            {
                _logger.LogWarning("Link path for entry '{Title}' posted at {PublishDate} was blank, ignoring",
                    item.Title.Text, item.PublishDate);
                continue;
            }

            var entryExists = await _dbContext.Entries
                .AsNoTracking().AnyAsync(e => e.RelPermalink == relPermalink, token);
            if (entryExists) continue;

            _dbContext.Entries.Add(new Entry { RelPermalink = relPermalink });
            await _dbContext.SaveChangesAsync(token);
        }
    }
}
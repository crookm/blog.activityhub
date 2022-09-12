using System.Net;

namespace Blog.ActivityHub.Api.Data.Models;

public class Reaction
{
    public int Id { get; set; }
    public Entry Entry { get; set; } = null!;
    public Blog.ActivityHub.Contracts.Reaction ReactionType { get; set; }
    public IPAddress IpAddress { get; set; } = null!;
    public DateTime Created { get; set; } = DateTimeOffset.Now.UtcDateTime;
    public DateTime? Removed { get; set; }
}
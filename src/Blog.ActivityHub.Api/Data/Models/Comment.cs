using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Blog.ActivityHub.Api.Data.Models;

public class Comment
{
    public int Id { get; set; }
    public Entry Entry { get; set; } = null!;
    public IPAddress IpAddress { get; set; } = null!;
    [MaxLength(25)] public string Name { get; set; } = null!;
    [MaxLength(256)] public string Content { get; set; } = null!;
    public DateTime Created { get; set; } = DateTimeOffset.Now.UtcDateTime;
    public DateTime? Removed { get; set; }
}
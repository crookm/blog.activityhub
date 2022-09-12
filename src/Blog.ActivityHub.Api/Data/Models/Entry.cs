namespace Blog.ActivityHub.Api.Data.Models;

public class Entry
{
    public int Id { get; set; }
    public string RelPermalink { get; set; } = null!;
    public DateTime Created { get; set; } = DateTimeOffset.Now.UtcDateTime;
}
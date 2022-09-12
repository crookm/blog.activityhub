using System.Diagnostics.CodeAnalysis;

namespace Blog.ActivityHub.Api.Options;

[ExcludeFromCodeCoverage]
public class BlogOptions
{
    public string BaseAddress { get; set; } = null!;
    public string FeedPath { get; set; } = null!;
}
using Blog.ActivityHub.Api.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Blog.ActivityHub.Api.Data;

public class ActivityDbContext : DbContext
{
    public ActivityDbContext(DbContextOptions<ActivityDbContext> dbContextOptions) : base(dbContextOptions)
    {
    }

    public DbSet<Entry> Entries { get; set; } = null!;
    public DbSet<Reaction> Reactions { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
}
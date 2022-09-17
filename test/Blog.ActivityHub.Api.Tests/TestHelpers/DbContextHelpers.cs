using System.Runtime.CompilerServices;
using Blog.ActivityHub.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Blog.ActivityHub.Api.Tests.TestHelpers;

public static class DbContextHelpers
{
    internal static ActivityDbContext CreateDbContext([CallerMemberName] string? methodName = "")
        => new(new DbContextOptionsBuilder<ActivityDbContext>()
            .UseInMemoryDatabase($"UnitTest-{methodName}", b => b.EnableNullChecks(false))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);
}
using Aoraki.Events.Publisher.Extensions;
using Blog.ActivityHub.Api.Contracts;
using Blog.ActivityHub.Api.Data;
using Blog.ActivityHub.Api.Endpoints;
using Blog.ActivityHub.Api.Options;
using Blog.ActivityHub.Api.Services;
using Blog.ActivityHub.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Routing services
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcHealthChecks();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options
    .AddDefaultPolicy(policy => policy
        .WithOrigins(
            "https://activityhub-app.mattcrook.io", // web
            "https://localhost:7233") // local: web
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(30))
        .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")));

// Data
builder.Services.AddDbContext<ActivityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ActivityDbContext"))
        .UseSnakeCaseNamingConvention());

// Configuration
builder.Services.Configure<BlogOptions>(builder.Configuration.GetSection("Blog"));

// Hosted services
builder.Services.AddHostedService<BlogMonitorWorker>();

// Other services
builder.Services.AddAorakiEventsPublisher(builder.Configuration["Events:Endpoint"]);
builder.Services.AddScoped<IBlogMonitorService, BlogMonitorService>();
builder.Services.AddScoped<IReactionService, ReactionService>();

var app = builder.Build();
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.UseCors();

app.MapGet("/", () => Results.Ok("nobody is home"));

// gRPC
app.MapGrpcHealthChecksService();
app.MapGrpcService<DiscussionEndpoint>();
app.MapGrpcService<EntryEndpoint>();

// Allow auto-discovery of gRPC services when running in dev mode
if (app.Environment.IsDevelopment())
    app.MapGrpcReflectionService();

await using (var scope = app.Services.CreateAsyncScope())
{
    // Apply migrations during app startup
    //  > Don't do this if there's ever more than one running app instance
    var db = scope.ServiceProvider.GetRequiredService<ActivityDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
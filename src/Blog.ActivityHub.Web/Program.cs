using Blog.ActivityHub.Contracts;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blog.ActivityHub.Web;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiUri = !builder.HostEnvironment.IsDevelopment()
    ? "https://activityhub-api.mattcrook.io"
    : "https://localhost:7279";

// ---
//  Api HttpClient Setup
// ---
builder.Services.AddHttpClient("Api",
        client => client.BaseAddress = new Uri(apiUri))
    .AddHttpMessageHandler(sp => new GrpcWebHandler(GrpcWebMode.GrpcWeb));

// ---
//  gRPC
// ---
builder.Services.AddSingleton<ChannelBase>(sp => GrpcChannel.ForAddress(apiUri,
    new GrpcChannelOptions { HttpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api") }));
builder.Services.AddTransient<Discussion.DiscussionClient>();
builder.Services.AddTransient<Entry.EntryClient>();

await builder.Build().RunAsync();
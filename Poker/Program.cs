using Poker.Hubs;
using Poker.Models;
using Poker.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddOpenApi();
builder.Services.AddScoped<Poker.Models.PokerGame>(); // Changed from AddSingleton to AddScoped
// builder.Services.AddSingleton<Poker.Models.Poker>();

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new CardJsonConverter());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // Listen on 0.0.0.0:80
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve static files from wwwroot
app.UseStaticFiles();
app.Urls.Clear();

// Configure SignalR

// Fallback to index.html for client-side routing
app.MapFallbackToFile("index.html");

// Configure the URLs
// app.Urls.Add("http://localhost:8080");
// app.Urls.Add("https://localhost:7160");

app.MapHub<PokerHub>("/pokerhub");
app.MapGet("/games", () =>
{
    return PokerHub.Games.Select(g => new
    {
        Id = g.Key,
        Players = g.Value.Players.Select(p => new
        {
            p.Name,
            p.ConnectionId
        })
    });
});
app.Run();


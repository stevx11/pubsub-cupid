using HaoticniKupidon.Server.Background;
using HaoticniKupidon.Server.Hubs;
using HaoticniKupidon.Server.Interfaces;
using HaoticniKupidon.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole(opts =>
{
    opts.FormatterName = "simple";
});

// DI

builder.Services.AddSingleton<IPersonStore, PersonStore>();

builder.Services.AddTransient<IMatchingService, MatchingService>();

builder.Services.AddScoped<ICupidService, CupidService>();

builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = true;
    opts.HandshakeTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHostedService<CupidBackgroundService>();

builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5000", "http://localhost:5001",
                         "https://localhost:5001")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseRouting();

app.MapHub<KupidonHub>("/kupidonHub");

Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("");
Console.WriteLine("     HAOTICNI KUPIDON SERVER     ");
Console.WriteLine("------------------------------------------");
Console.WriteLine("   Pisma se salju svakih 60 sekundi        ");
Console.WriteLine("");

Console.ResetColor();

app.Run();
using DbTraffic.Core.Repositories;
using DbTraffic.Core.Rules;
using DbTraffic.Core.Services;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Discovery;
using DbTraffic.Infrastructure.Monitoring;
using DbTraffic.Infrastructure.Repositories;
using DbTraffic.Infrastructure.Services;
using DbTraffic.Infrastructure.SqlServer;
using DbTraffic.Shared.Models;
using DbTraffic.Web.Components;
using DbTraffic.Web.Endpoints;
using DbTraffic.Web.Middleware;
using Microsoft.AspNetCore.Components.Server;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CircuitOptions>(options => options.DetailedErrors = true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
});

builder.Services.AddHttpClient();

builder.Services.AddSingleton<InstanceConnectionInfo>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new InstanceConnectionInfo
    {
        Id = Guid.NewGuid(),
        Name = configuration["DbTraffic:DemoInstance:Name"] ?? "Demo",
        ConnectionString = configuration["DbTraffic:DemoInstance:ConnectionString"] ?? string.Empty
    };
});

builder.Services.AddScoped<ISqlServerInstanceClient, SqlServerInstanceClient>();
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<DatabaseInitializer>();
builder.Services.AddScoped<IProcessRepository, ProcessRepository>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddScoped<IExecutionRepository, ExecutionRepository>();
builder.Services.AddScoped<IInstanceSnapshotRepository, InstanceSnapshotRepository>();
builder.Services.AddScoped<DiscoveryService>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IExecutionService, ExecutionService>();
builder.Services.Configure<DiscoveryWorkerOptions>(options =>
{
    options.Interval = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("DbTraffic:Discovery:IntervalMinutes", 60));
});
builder.Services.Configure<MonitoringWorkerOptions>(options =>
{
    options.Interval = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("DbTraffic:Monitoring:IntervalMinutes", 5));
    options.Retention = TimeSpan.FromDays(builder.Configuration.GetValue<int>("DbTraffic:Monitoring:RetentionDays", 7));
});
builder.Services.AddHostedService<DiscoveryWorker>();
builder.Services.AddHostedService<MonitoringWorker>();

// Rules engine
builder.Services.AddScoped<IRule, ObjectOverlapRule>();
builder.Services.AddScoped<IRule, HighIntensityOverlapRule>();
builder.Services.AddScoped<IRule, EstimatedDurationExceedsWindowRule>();
builder.Services.AddScoped<IRule, InstanceResourcePressureRule>();
builder.Services.AddScoped<IRiskCalculationService, RiskCalculationService>();
builder.Services.AddScoped<IRiskContextProvider, RiskContextProvider>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();
app.UseMiddleware<DomainExceptionMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Lightweight health check endpoint for load testing and general uptime
app.MapGet("/api/health", () => Results.Ok(new { Status = "Healthy", Version = "1.0.0" }));

// Health check endpoint for SQL Server connectivity (MVP phase 0)
app.MapGet("/api/health/sql", async (ISqlServerInstanceClient client, CancellationToken cancellationToken) =>
{
    var canConnect = await client.CanConnectAsync(cancellationToken);
    if (!canConnect)
    {
        return Results.Problem("Cannot connect to the configured SQL Server instance.");
    }

    var requests = await client.GetActiveRequestsAsync(cancellationToken);
    return Results.Ok(new
    {
        Connected = true,
        ActiveRequestCount = requests.Count,
        Requests = requests.Take(10)
    });
});

app.MapInstanceEndpoints();
app.MapProcessEndpoints();
app.MapDiscoveryEndpoints();
app.MapRiskEndpoints();
app.MapRecommendationEndpoints();
app.MapMonitoringEndpoints();
app.MapExecutionEndpoints();
app.MapDashboardEndpoints();

// Ensure the product database schema is applied before accepting requests.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();

public partial class Program { }


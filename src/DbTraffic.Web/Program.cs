using DbTraffic.Core.Repositories;
using DbTraffic.Infrastructure.Data;
using DbTraffic.Infrastructure.Discovery;
using DbTraffic.Infrastructure.Repositories;
using DbTraffic.Infrastructure.SqlServer;
using DbTraffic.Shared.Models;
using DbTraffic.Web.Components;
using DbTraffic.Web.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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
builder.Services.AddScoped<IProcessRepository, ProcessRepository>();
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<IDiscoveryRepository, DiscoveryRepository>();
builder.Services.AddScoped<DiscoveryService>();
builder.Services.Configure<DiscoveryWorkerOptions>(options =>
{
    options.Interval = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("DbTraffic:Discovery:IntervalMinutes", 60));
});
builder.Services.AddHostedService<DiscoveryWorker>();

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

app.Run();

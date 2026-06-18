using Pinmo.Api;
using Pinmo.Core;
using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure;
using Pinmo.Infrastructure.Services;
using Pinmo.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

var appDataPath = ResolveAppDataPath(builder.Configuration);
var port = builder.Configuration.GetValue("Pinmo:Port", 5199);

builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddPinmoInfrastructure(appDataPath);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var endpointStore = scope.ServiceProvider.GetRequiredService<InMemoryEndpointStore>();
    var settingsStore = scope.ServiceProvider.GetRequiredService<InMemorySettingsStore>();

    await endpointStore.LoadSeedFromFileAsync(DependencyInjection.GetEndpointsSeedPath(appDataPath));
    await settingsStore.LoadSeedFromFileAsync(DependencyInjection.GetSettingsSeedPath(appDataPath));
}

app.UseCors();

var api = app.MapGroup("/api");

api.MapGet("/dashboard", async (IEndpointStore endpointStore, IPingRecordStore pingRecordStore) =>
    Results.Ok(await BuildDashboardSummaryAsync(endpointStore, pingRecordStore)));

api.MapPost("/dashboard/reset", async (
    IEndpointStore endpointStore,
    IPingRecordStore pingRecordStore,
    MonitoringScheduleState scheduleState) =>
{
    await pingRecordStore.ClearAllAsync();
    await endpointStore.ResetAllPingStateAsync();
    scheduleState.ResetSchedule();

    return Results.Ok(await BuildDashboardSummaryAsync(endpointStore, pingRecordStore));
});

api.MapGet("/endpoints", async (IEndpointStore endpointStore) =>
{
    var endpoints = await endpointStore.GetAllAsync();
    return Results.Ok(endpoints.Select(e => e.ToResponse()));
});

api.MapGet("/endpoints/{id:guid}", async (Guid id, IEndpointStore endpointStore) =>
{
    var endpoint = await endpointStore.GetByIdAsync(id);
    return endpoint is null ? Results.NotFound() : Results.Ok(endpoint.ToResponse());
});

api.MapPost("/endpoints", async (EndpointRequest request, IEndpointStore endpointStore) =>
{
    if (!EndpointAddress.TryNormalize(request.Url, out var url, out var errorMessage))
    {
        return Results.BadRequest(new { message = errorMessage });
    }

    var endpoint = new MonitoredEndpoint
    {
        Id = Guid.NewGuid(),
        Name = EndpointMapper.DeriveNameFromUrl(url),
        Url = url,
        HttpMethod = "GET",
        IsEnabled = true,
        CreatedAt = DateTime.UtcNow
    };

    await endpointStore.AddAsync(endpoint);

    return Results.Created($"/api/endpoints/{endpoint.Id}", endpoint.ToResponse());
});

api.MapPut("/endpoints/{id:guid}", async (Guid id, EndpointRequest request, IEndpointStore endpointStore) =>
{
    var endpoint = await endpointStore.GetByIdAsync(id);
    if (endpoint is null)
    {
        return Results.NotFound();
    }

    if (!EndpointAddress.TryNormalize(request.Url, out var url, out var errorMessage))
    {
        return Results.BadRequest(new { message = errorMessage });
    }

    endpoint.Url = url;
    endpoint.Name = EndpointMapper.DeriveNameFromUrl(url);

    await endpointStore.UpdateAsync(endpoint);
    return Results.Ok(endpoint.ToResponse());
});

api.MapDelete("/endpoints/{id:guid}", async (
    Guid id,
    IEndpointStore endpointStore,
    IPingRecordStore pingRecordStore) =>
{
    var deleted = await endpointStore.DeleteAsync(id);
    if (!deleted)
    {
        return Results.NotFound();
    }

    await pingRecordStore.RemoveForEndpointAsync(id);

    return Results.NoContent();
});

api.MapPost("/endpoints/{id:guid}/ping", async (
    Guid id,
    IEndpointStore endpointStore,
    IEndpointPingOrchestrator pingOrchestrator) =>
{
    var endpoint = await endpointStore.GetByIdAsync(id);
    if (endpoint is null)
    {
        return Results.NotFound();
    }

    await pingOrchestrator.PingAndRecordAsync(endpoint, CancellationToken.None);
    var updated = await endpointStore.GetByIdAsync(id);

    return Results.Ok(updated!.ToResponse());
});

api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

api.MapGet("/settings", async (ISettingsStore settingsStore) =>
{
    var settings = await settingsStore.GetAsync();
    return Results.Ok(ToSettingsResponse(settings));
});

api.MapPut("/settings", async (SettingsUpdateRequest request, ISettingsStore settingsStore) =>
{
    if (!TryParseCloseWindowAction(request.CloseWindowAction, out var closeAction))
    {
        return Results.BadRequest(new { message = "Invalid close window action." });
    }

    var settings = await settingsStore.GetAsync();
    settings.CloseWindowAction = closeAction;
    var saved = await settingsStore.SaveAsync(settings);
    return Results.Ok(ToSettingsResponse(saved));
});

app.Run();

static async Task<DashboardSummary> BuildDashboardSummaryAsync(
    IEndpointStore endpointStore,
    IPingRecordStore pingRecordStore)
{
    var endpoints = await endpointStore.GetAllAsync();
    var endpointIds = endpoints.Select(e => e.Id).ToList();
    var records = await pingRecordStore.GetForEndpointsAsync(endpointIds);

    var pingStats = records
        .GroupBy(r => r.MonitoredEndpointId)
        .ToDictionary(
            g => g.Key,
            g => new
            {
                AvgPingMs = g.Where(r => r.PacketsSucceeded > 0)
                    .Average(r => (double?)r.ResponseTimeMs),
                AvgPacketLossPercent = g.Average(r =>
                    r.PacketsSent > 0
                        ? (double)(r.PacketsSent - r.PacketsSucceeded) / r.PacketsSent * 100
                        : r.IsSuccess ? 0 : 100)
            });

    var rows = endpoints.Select(endpoint =>
    {
        pingStats.TryGetValue(endpoint.Id, out var stats);
        var latestPingMs = endpoint.LastIsSuccess == true && endpoint.LastResponseTimeMs > 0
            ? endpoint.LastResponseTimeMs
            : null;

        return new DashboardEndpointRow(
            endpoint.Id,
            endpoint.Url,
            latestPingMs,
            stats?.AvgPingMs is null ? null : Math.Round(stats.AvgPingMs.Value, 1),
            stats is null ? null : Math.Round(stats.AvgPacketLossPercent, 1));
    }).ToList();

    return new DashboardSummary(rows);
}

static SettingsResponse ToSettingsResponse(AppSettings settings) =>
    new(settings.RequestTimeoutSeconds, ToCloseWindowActionValue(settings.CloseWindowAction));

static string ToCloseWindowActionValue(CloseWindowAction action) =>
    action == CloseWindowAction.MinimizeToTaskbar ? "minimizeToTaskbar" : "quit";

static bool TryParseCloseWindowAction(string? value, out CloseWindowAction action)
{
    if (string.Equals(value, "minimizeToTaskbar", StringComparison.OrdinalIgnoreCase))
    {
        action = CloseWindowAction.MinimizeToTaskbar;
        return true;
    }

    if (string.Equals(value, "quit", StringComparison.OrdinalIgnoreCase))
    {
        action = CloseWindowAction.Quit;
        return true;
    }

    action = CloseWindowAction.Quit;
    return false;
}

static string ResolveAppDataPath(IConfiguration configuration)
{
    var configuredPath = configuration["Pinmo:AppDataPath"];
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
        return Path.GetFullPath(configuredPath);
    }

    var fromWorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "app");
    if (Directory.Exists(fromWorkingDirectory))
    {
        return Path.GetFullPath(fromWorkingDirectory);
    }

    var fromContentRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "app");
    if (Directory.Exists(fromContentRoot))
    {
        return Path.GetFullPath(fromContentRoot);
    }

    return Path.GetFullPath(fromWorkingDirectory);
}

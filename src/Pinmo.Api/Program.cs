using Microsoft.EntityFrameworkCore;
using Pinmo.Api;
using Pinmo.Core;
using Pinmo.Core.Dtos;
using Pinmo.Core.Entities;
using Pinmo.Core.Interfaces;
using Pinmo.Infrastructure;
using Pinmo.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Pinmo");
Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "pinmo.db");
var port = builder.Configuration.GetValue("Pinmo:Port", 5199);

builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddPinmoInfrastructure(databasePath);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PinmoDbContext>();
    await DbSchemaPatcher.ApplyAsync(db);
}

app.UseCors();

var api = app.MapGroup("/api");

api.MapGet("/dashboard", async (PinmoDbContext db) =>
{
    var endpoints = await db.MonitoredEndpoints
        .AsNoTracking()
        .OrderBy(e => e.Name)
        .ToListAsync();

    var enabled = endpoints.Where(e => e.IsEnabled).ToList();
    var upCount = enabled.Count(e => e.LastIsSuccess == true);
    var downCount = enabled.Count(e => e.LastIsSuccess == false);
    var unknownCount = enabled.Count(e => e.LastIsSuccess is null);

    var responseTimes = enabled
        .Where(e => e.LastResponseTimeMs.HasValue)
        .Select(e => e.LastResponseTimeMs!.Value)
        .ToList();

    var averageResponseTime = responseTimes.Count > 0
        ? responseTimes.Average()
        : 0;

    return Results.Ok(new DashboardSummary(
        endpoints.Count,
        enabled.Count,
        upCount,
        downCount,
        unknownCount,
        Math.Round(averageResponseTime, 1),
        endpoints.Select(e => e.ToResponse()).ToList()));
});

api.MapGet("/endpoints", async (PinmoDbContext db) =>
{
    var endpoints = await db.MonitoredEndpoints
        .AsNoTracking()
        .OrderBy(e => e.Name)
        .ToListAsync();

    return Results.Ok(endpoints.Select(e => e.ToResponse()));
});

api.MapGet("/endpoints/{id:guid}", async (Guid id, PinmoDbContext db) =>
{
    var endpoint = await db.MonitoredEndpoints.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    return endpoint is null ? Results.NotFound() : Results.Ok(endpoint.ToResponse());
});

api.MapPost("/endpoints", async (EndpointRequest request, PinmoDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new { message = "Name and URL are required." });
    }

    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new { message = "URL must be a valid absolute URI." });
    }

    var settings = await db.AppSettings.AsNoTracking().FirstAsync();
    var interval = MonitoringOptions.NormalizeInterval(
        request.IntervalSeconds > 0 ? request.IntervalSeconds : settings.DefaultIntervalSeconds);
    var packetsPerPing = MonitoringOptions.NormalizePacketCount(request.PacketsPerPing);

    var endpoint = new MonitoredEndpoint
    {
        Id = Guid.NewGuid(),
        Name = request.Name.Trim(),
        Url = request.Url.Trim(),
        HttpMethod = string.IsNullOrWhiteSpace(request.HttpMethod) ? "GET" : request.HttpMethod.ToUpperInvariant(),
        IntervalSeconds = interval,
        PacketsPerPing = packetsPerPing,
        IsEnabled = request.IsEnabled,
        CreatedAt = DateTime.UtcNow
    };

    db.MonitoredEndpoints.Add(endpoint);
    await db.SaveChangesAsync();

    return Results.Created($"/api/endpoints/{endpoint.Id}", endpoint.ToResponse());
});

api.MapPut("/endpoints/{id:guid}", async (Guid id, EndpointRequest request, PinmoDbContext db) =>
{
    var endpoint = await db.MonitoredEndpoints.FirstOrDefaultAsync(e => e.Id == id);
    if (endpoint is null)
    {
        return Results.NotFound();
    }

    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Url))
    {
        return Results.BadRequest(new { message = "Name and URL are required." });
    }

    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new { message = "URL must be a valid absolute URI." });
    }

    endpoint.Name = request.Name.Trim();
    endpoint.Url = request.Url.Trim();
    endpoint.HttpMethod = string.IsNullOrWhiteSpace(request.HttpMethod) ? "GET" : request.HttpMethod.ToUpperInvariant();
    endpoint.IntervalSeconds = request.IntervalSeconds > 0
        ? MonitoringOptions.NormalizeInterval(request.IntervalSeconds)
        : endpoint.IntervalSeconds;
    endpoint.PacketsPerPing = MonitoringOptions.NormalizePacketCount(request.PacketsPerPing);
    endpoint.IsEnabled = request.IsEnabled;

    await db.SaveChangesAsync();
    return Results.Ok(endpoint.ToResponse());
});

api.MapDelete("/endpoints/{id:guid}", async (Guid id, PinmoDbContext db) =>
{
    var endpoint = await db.MonitoredEndpoints.FirstOrDefaultAsync(e => e.Id == id);
    if (endpoint is null)
    {
        return Results.NotFound();
    }

    db.MonitoredEndpoints.Remove(endpoint);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

api.MapPost("/endpoints/{id:guid}/ping", async (
    Guid id,
    PinmoDbContext db,
    IEndpointPingOrchestrator pingOrchestrator) =>
{
    var endpoint = await db.MonitoredEndpoints.FirstOrDefaultAsync(e => e.Id == id);
    if (endpoint is null)
    {
        return Results.NotFound();
    }

    await pingOrchestrator.PingAndRecordAsync(endpoint, CancellationToken.None);
    return Results.Ok(endpoint.ToResponse());
});

api.MapGet("/history", async (
    PinmoDbContext db,
    int page = 1,
    int pageSize = 50,
    Guid? endpointId = null,
    bool? successOnly = null) =>
{
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 200);

    var query = db.PingRecords
        .AsNoTracking()
        .Include(r => r.MonitoredEndpoint)
        .AsQueryable();

    if (endpointId.HasValue)
    {
        query = query.Where(r => r.MonitoredEndpointId == endpointId.Value);
    }

    if (successOnly.HasValue)
    {
        query = query.Where(r => r.IsSuccess == successOnly.Value);
    }

    var totalCount = await query.CountAsync();
    var records = await query
        .OrderByDescending(r => r.CheckedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(r => new PingRecordResponse(
            r.Id,
            r.MonitoredEndpointId,
            r.MonitoredEndpoint.Name,
            r.MonitoredEndpoint.Url,
            r.CheckedAt,
            r.IsSuccess,
            r.StatusCode,
            r.ResponseTimeMs,
            r.ErrorMessage))
        .ToListAsync();

    return Results.Ok(new
    {
        page,
        pageSize,
        totalCount,
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
        records
    });
});

api.MapDelete("/history", async (PinmoDbContext db) =>
{
    var settings = await db.AppSettings.AsNoTracking().FirstAsync();
    var cutoff = DateTime.UtcNow.AddDays(-settings.HistoryRetentionDays);
    var oldRecords = await db.PingRecords.Where(r => r.CheckedAt < cutoff).ToListAsync();

    if (oldRecords.Count == 0)
    {
        return Results.Ok(new { deletedCount = 0 });
    }

    db.PingRecords.RemoveRange(oldRecords);
    await db.SaveChangesAsync();
    return Results.Ok(new { deletedCount = oldRecords.Count });
});

api.MapGet("/settings", async (PinmoDbContext db) =>
{
    var settings = await db.AppSettings.AsNoTracking().FirstAsync();
    return Results.Ok(settings.ToResponse());
});

api.MapPut("/settings", async (SettingsRequest request, PinmoDbContext db) =>
{
    var settings = await db.AppSettings.FirstAsync();

    settings.DefaultIntervalSeconds = MonitoringOptions.NormalizeInterval(request.DefaultIntervalSeconds);
    settings.RequestTimeoutSeconds = Math.Clamp(request.RequestTimeoutSeconds, 1, 120);
    settings.HistoryRetentionDays = Math.Max(1, request.HistoryRetentionDays);
    settings.StartMonitoringOnLaunch = request.StartMonitoringOnLaunch;
    settings.NotifyOnFailure = request.NotifyOnFailure;

    await db.SaveChangesAsync();
    return Results.Ok(settings.ToResponse());
});

api.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

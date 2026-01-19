using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WhearApp.Infrastructure.Database;
using WhearApp.WebApi.Extensions;

namespace WhearApp.WebApi.Endpoints.System;


/// <summary>
///     Health check endpoints for monitoring system status
/// </summary>
public static class HealthEndpoints
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder group)
    {
        // Basic health check
        group.MapGet("/", GetHealth)
            .WithName("GetHealth")
            .WithApiMetadata("Get basic health status", "Returns basic health status of the API")
            .Produces<HealthResponse>()
            .ExcludeFromDescription();

        // Detailed health check with dependencies
        group.MapGet("/detailed", GetDetailedHealth)
            .WithName("GetDetailedHealth")
            .WithApiMetadata("Get detailed health status",
                "Returns detailed health status including database and external services")
            .Produces<DetailedHealthResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status503ServiceUnavailable);

        return group;
    }

    private static IResult GetHealth()
    {
        return Results.Ok(new HealthResponse
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    private static async Task<IResult> GetDetailedHealth(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        IConnectionMultiplexer redis,
        IHostEnvironment environment)
    {
        var checks = new Dictionary<string, ComponentHealth>
        {
            // Check database
            ["database"] = await CheckDatabaseAsync(dbContext),
            ["cache"] = await CheckCacheAsync(redis),
        };

        // Overall status
        var isHealthy = checks.Values.All(c => c.Status == "healthy");
        var overallStatus = isHealthy ? "healthy" : "degraded";

        var response = new DetailedHealthResponse
        {
            Status = overallStatus,
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = environment.EnvironmentName,
            Checks = checks
        };

        return isHealthy
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<ComponentHealth> CheckDatabaseAsync(ApplicationDbContext dbContext)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simple query to check database connectivity
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            stopwatch.Stop();

            return new ComponentHealth
            {
                Status = "healthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["provider"] = "PostgreSQL",
                    ["database"] = dbContext.Database.GetDbConnection().Database
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new ComponentHealth
            {
                Status = "unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
    
    private static async Task<ComponentHealth> CheckCacheAsync(IConnectionMultiplexer redis)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var db = redis.GetDatabase();
            var latency = await db.PingAsync();
            stopwatch.Stop();

            var endpoint = redis.GetEndPoints().FirstOrDefault();
            var server = endpoint is not null ? redis.GetServer(endpoint) : null;

            return new ComponentHealth
            {
                Status = "healthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["provider"] = "Redis",
                    ["latencyMs"] = latency.TotalMilliseconds,
                    ["endpoint"] = endpoint?.ToString() ?? "unknown",
                    ["connected"] = redis.IsConnected,
                    ["serverVersion"] = server?.Version.ToString() ?? "unknown"
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                Status = "unhealthy",
                ResponseTime = stopwatch.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }
}

// ==================== RESPONSE MODELS ====================

public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
}

public class DetailedHealthResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public Dictionary<string, ComponentHealth> Checks { get; set; } = new();
}

public class ComponentHealth
{
    public string Status { get; set; } = "healthy";
    public long ResponseTime { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FacturArtisan.Api.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = (int)Math.Ceiling(report.TotalDuration.TotalMilliseconds),
            checks = report.Entries.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString(),
                description = kvp.Value.Description,
                durationMs = (int)Math.Ceiling(kvp.Value.Duration.TotalMilliseconds),
                error = kvp.Value.Exception?.Message,
                data = kvp.Value.Data.Count == 0 ? null : kvp.Value.Data
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

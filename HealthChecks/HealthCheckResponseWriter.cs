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
            checks = report.Entries.Select(kvp => new
            {
                name = kvp.Key,
                status = kvp.Value.Status.ToString()
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

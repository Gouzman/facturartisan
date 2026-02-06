using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FacturArtisan.Api.HealthChecks;

public sealed class MemoryHealthCheck : IHealthCheck
{
    private readonly long _maxBytes;

    public MemoryHealthCheck(IConfiguration configuration)
    {
        var maxMb = configuration.GetValue<long?>("HealthChecks:MemoryMaxMb")
                    ?? ParseEnvLong("MEMORY_HEALTH_MAX_MB")
                    ?? 1024;

        if (maxMb <= 0) maxMb = 1024;
        _maxBytes = maxMb * 1024 * 1024;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);

        var data = new Dictionary<string, object>
        {
            ["allocatedBytes"] = allocatedBytes,
            ["maxBytes"] = _maxBytes,
            ["allocatedMb"] = Math.Round(allocatedBytes / 1024d / 1024d, 2),
            ["maxMb"] = Math.Round(_maxBytes / 1024d / 1024d, 2)
        };

        if (allocatedBytes <= _maxBytes)
            return Task.FromResult(HealthCheckResult.Healthy(data: data));

        return Task.FromResult(HealthCheckResult.Unhealthy("Allocated memory above threshold.", data: data));
    }

    private static long? ParseEnvLong(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return long.TryParse(raw, out var value) ? value : null;
    }
}

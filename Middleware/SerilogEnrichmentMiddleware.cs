using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Serilog.Context;

namespace FacturArtisan.Api.Middleware;

public sealed class SerilogEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public SerilogEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = context.TraceIdentifier;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var userId = context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? context.User?.FindFirstValue("sub")
                     ?? context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("IP", ip))
        using (LogContext.PushProperty("UserId", userId ?? string.Empty))
        {
            await _next(context);
        }
    }
}

using FacturArtisan.Api.Data;
using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.Services;
using FacturArtisan.Api.HealthChecks;
using FacturArtisan.Api.Middleware;
using FacturArtisan.Api.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Serilog (config via appsettings)
builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// DEV fallback (sans secrets commit): dotnet user-secrets
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// --- Configuration via variables d'environnement ---
string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);

var dbHost = GetEnv("DB_HOST");
var dbPort = GetEnv("DB_PORT");
var dbName = GetEnv("DB_NAME");
var dbUser = GetEnv("DB_USER");
var dbPassword = GetEnv("DB_PASSWORD");

var jwtKeyEnv = GetEnv("JWT_KEY");

// DB: priorité aux env vars; fallback DEV seulement via config (appsettings.Development.json ou user-secrets)
string? connectionString = null;
var hasDbEnv = !string.IsNullOrWhiteSpace(dbHost)
             && !string.IsNullOrWhiteSpace(dbName)
             && !string.IsNullOrWhiteSpace(dbUser)
             && !string.IsNullOrWhiteSpace(dbPassword);

if (hasDbEnv)
{
    var csb = new NpgsqlConnectionStringBuilder
    {
        Host = dbHost,
        Database = dbName,
        Username = dbUser,
        Password = dbPassword
    };

    if (int.TryParse(dbPort, out var port))
        csb.Port = port;

    connectionString = csb.ConnectionString;
}
else if (builder.Environment.IsDevelopment())
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DB configuration missing. Set DB_HOST, DB_NAME, DB_USER, DB_PASSWORD (and optional DB_PORT). " +
        "In Development you may also set ConnectionStrings:DefaultConnection via user-secrets."
    );
}

// JWT: priorité à JWT_KEY; fallback DEV seulement via config (user-secrets)
string? jwtKey = !string.IsNullOrWhiteSpace(jwtKeyEnv)
    ? jwtKeyEnv
    : (builder.Environment.IsDevelopment() ? builder.Configuration["Jwt:Key"] : null);

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT key missing. Set JWT_KEY environment variable. " +
        "In Development you may also set Jwt:Key via user-secrets."
    );
}

// Injecte les valeurs résolues dans IConfiguration pour réutiliser JwtTokenService & EF config
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:DefaultConnection"] = connectionString,
    ["Jwt:Key"] = jwtKey
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers
builder.Services.AddControllers();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        if (!context.HttpContext.Response.HasStarted)
        {
            var traceId = context.HttpContext.TraceIdentifier;

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Trop de requêtes, veuillez réessayer plus tard.",
                Instance = context.HttpContext.Request.Path
            };

            problem.Extensions["traceId"] = traceId;

            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
        }
    };

    // Global: 100 req/min/IP (mais on laisse Swagger + health accessibles)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/health", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/db-health", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("public");
        }

        // Applique le global uniquement sur l'API
        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetNoLimiter("non-api");
        }

        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("auth-login", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"login:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("auth-register", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"register:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// Reverse proxy (Nginx) forwarded headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    // Par défaut, on fait confiance au proxy local (Nginx en reverse proxy sur la même machine).
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);

    // Production: si ton reverse-proxy n'est pas en loopback (Docker/VM/autre host), configure:
    // TRUSTED_PROXIES="127.0.0.1,10.0.0.10"
    // TRUSTED_NETWORKS="172.17.0.0/16,10.0.0.0/8"
    var trustedProxies = Environment.GetEnvironmentVariable("TRUSTED_PROXIES");
    if (!string.IsNullOrWhiteSpace(trustedProxies))
    {
        foreach (var ipStr in trustedProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IPAddress.TryParse(ipStr, out var ip))
                options.KnownProxies.Add(ip);
        }
    }

    var trustedNetworks = Environment.GetEnvironmentVariable("TRUSTED_NETWORKS");
    if (!string.IsNullOrWhiteSpace(trustedNetworks))
    {
        foreach (var cidr in trustedNetworks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = cidr.Split('/', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2) continue;
            if (!IPAddress.TryParse(parts[0], out var baseAddress)) continue;
            if (!int.TryParse(parts[1], out var prefixLength)) continue;

            try
            {
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(baseAddress, prefixLength));
            }
            catch
            {
                // Ignore invalid CIDR
            }
        }
    }
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCors", policy =>
    {
        policy
            .WithOrigins(
                "https://facturartisan.online",
                "https://app.facturartisan.online"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Auth
builder.Services.AddScoped<JwtTokenService>();

// Application services (Controllers -> Services -> DbContext)
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
builder.Services.AddScoped<IDevisService, DevisService>();
builder.Services.AddScoped<IFactureService, FactureService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? string.Empty))
        };
    });

builder.Services.AddAuthorization();

// Health checks
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

if (builder.Configuration.GetValue<bool>("HealthChecks:UseDbContextCheck"))
{
    healthChecks.AddDbContextCheck<AppDbContext>(name: "db");
}
else
{
    healthChecks.AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "db");
}

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FacturArtisan.Api", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Entrer 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, new List<string>() }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

// Enrich logs inside request scope (RequestId/UserId/IP)
app.UseMiddleware<SerilogEnrichmentMiddleware>();

// Request logging (one log per HTTP request)
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
        diagnosticContext.Set("IP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        var userId = httpContext.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? httpContext.User?.FindFirstValue("sub")
                     ?? httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        diagnosticContext.Set("UserId", userId ?? string.Empty);
    };
});

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// Avec UseForwardedHeaders au-dessus, IsHttps est correct derrière Nginx.
app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();

var corsPolicyName = app.Environment.IsDevelopment() ? "DevelopmentCors" : "ProductionCors";

// Swagger middleware
var enableSwagger = app.Environment.IsDevelopment() ||
                    string.Equals(Environment.GetEnvironmentVariable("ENABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);

if (enableSwagger)
{
    // En PROD, protège /swagger via JWT (et optionnellement une allowlist d'emails admin)
    if (!app.Environment.IsDevelopment())
    {
        app.UseWhen(
            ctx => ctx.Request.Path.StartsWithSegments("/swagger"),
            branch =>
            {
                branch.Use(async (ctx, next) =>
                {
                    var authResult = await ctx.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                    if (!authResult.Succeeded || authResult.Principal == null)
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    var adminEmailsEnv = Environment.GetEnvironmentVariable("SWAGGER_ADMIN_EMAILS");
                    if (!string.IsNullOrWhiteSpace(adminEmailsEnv))
                    {
                        var email = authResult.Principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                                    ?? authResult.Principal.FindFirst("email")?.Value;

                        var isAllowed = adminEmailsEnv
                            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase));

                        if (!isAllowed)
                        {
                            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return;
                        }
                    }

                    await next();
                });
            });
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(corsPolicyName);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Routing
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteJson
});

app.MapHealthChecks("/db-health", new HealthCheckOptions
{
    Predicate = r => string.Equals(r.Name, "db", StringComparison.OrdinalIgnoreCase),
    ResponseWriter = HealthCheckResponseWriter.WriteJson
});

app.Run();

public partial class Program
{
}

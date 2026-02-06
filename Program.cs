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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<DbHealthCheck>("db")
    .AddCheck<MemoryHealthCheck>("memory");

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

app.MapHealthChecks("/memory-health", new HealthCheckOptions
{
    Predicate = r => string.Equals(r.Name, "memory", StringComparison.OrdinalIgnoreCase),
    ResponseWriter = HealthCheckResponseWriter.WriteJson
});

app.Run();

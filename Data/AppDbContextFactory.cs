using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FacturArtisan.Api.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? GetEnv(string name) => Environment.GetEnvironmentVariable(name);

        var dbHost = GetEnv("DB_HOST");
        var dbPort = GetEnv("DB_PORT");
        var dbName = GetEnv("DB_NAME");
        var dbUser = GetEnv("DB_USER");
        var dbPassword = GetEnv("DB_PASSWORD");

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
        else
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("No database configuration found for design-time DbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}

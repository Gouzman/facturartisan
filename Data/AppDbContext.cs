using Microsoft.EntityFrameworkCore;
using FacturArtisan.Api.Models;

namespace FacturArtisan.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {}

    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<ServiceItem> Services { get; set; } = null!;
}

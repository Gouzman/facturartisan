using Microsoft.EntityFrameworkCore;
using FacturArtisan.Api.Models;

namespace FacturArtisan.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {}

    public DbSet<Client> Clients { get; set; } = null!;
    public DbSet<ServiceItem> Services { get; set; } = null!;
    public DbSet<Devis> Devis { get; set; } = null!;
    public DbSet<DevisItem> DevisItems { get; set; } = null!;
    public DbSet<Facture> Factures { get; set; } = null!;

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Users / RefreshTokens ---
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.TokenHash)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Query performance indexes ---
        modelBuilder.Entity<Facture>()
            .HasIndex(f => f.CreatedAt);

        modelBuilder.Entity<Facture>()
            .HasIndex(f => f.Statut);

        modelBuilder.Entity<Facture>()
            .HasIndex(f => f.Numero)
            .IsUnique();

        modelBuilder.Entity<Devis>()
            .HasIndex(d => d.CreatedAt);
    }
}

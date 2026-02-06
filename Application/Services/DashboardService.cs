using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Data;
using FacturArtisan.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetMonthlyStatsUtc()
    {
        var nowUtc = DateTime.UtcNow;

        var debutMois = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMois = debutMois.AddMonths(1);

        var stats = await _db.Factures
            .AsNoTracking()
            .Where(f => f.CreatedAt >= debutMois && f.CreatedAt < finMois)
            .GroupBy(_ => 1)
            .Select(g => new DashboardStatsDto
            {
                TotalMois = g.Sum(f => f.Total),
                TotalEncaisse = g.Sum(f => f.Statut == "Payee" ? f.Total : 0),
                TotalEnAttente = g.Sum(f => f.Statut != "Payee" ? f.Total : 0),
                NombreFactures = g.Count()
            })
            .FirstOrDefaultAsync();

        return stats ?? new DashboardStatsDto();
    }
}

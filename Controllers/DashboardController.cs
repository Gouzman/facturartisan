using FacturArtisan.Api.Data;
using FacturArtisan.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var nowUtc = DateTime.UtcNow;

            var debutMois = new DateTime(
                nowUtc.Year, 
                nowUtc.Month, 
                1, 0, 0, 0, 
                DateTimeKind.Utc
            );

            var finMois = debutMois.AddMonths(1);

            var facturesMois = await _db.Factures
                .Where(f => f.CreatedAt != null)
                .Where(f => f.CreatedAt >= debutMois && f.CreatedAt < finMois)
                .ToListAsync();

            var totalMois = facturesMois.Sum(f => f.Total);
            var totalEncaisse = facturesMois
                .Where(f => f.Statut == "Payee")
                .Sum(f => f.Total);

            var totalEnAttente = facturesMois
                .Where(f => f.Statut != "Payee")
                .Sum(f => f.Total);

            return Ok(new DashboardStatsDto
            {
                TotalMois = totalMois,
                TotalEncaisse = totalEncaisse,
                TotalEnAttente = totalEnAttente,
                NombreFactures = facturesMois.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Dashboard stats error",
                details = ex.Message
            });
        }
    }
}

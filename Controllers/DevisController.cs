using FacturArtisan.Api.Data;
using FacturArtisan.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/devis")]
public class DevisController : ControllerBase
{
    private readonly AppDbContext _db;

    public DevisController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var devis = await _db.Devis
            .Include(d => d.Client)
            .Include(d => d.Items)
                .ThenInclude(i => i.ServiceItem)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(devis);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Devis devis)
    {
        devis.Total = devis.Items.Sum(i =>
        {
            i.Total = i.Quantite * i.PrixUnitaire;
            return i.Total;
        });

        _db.Devis.Add(devis);
        await _db.SaveChangesAsync();

        return Ok(devis);
    }
}

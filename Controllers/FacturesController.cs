using FacturArtisan.Api.Data;
using FacturArtisan.Api.Models;
using FacturArtisan.Api.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/factures")]
public class FacturesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FacturesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var factures = await _db.Factures
            .Include(f => f.Devis)
                .ThenInclude(d => d.Client)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return Ok(factures);
    }

    [HttpPost("from-devis/{devisId}")]
    public async Task<IActionResult> CreateFromDevis(Guid devisId)
    {
        var devis = await _db.Devis
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == devisId);

        if (devis == null)
            return NotFound("Devis introuvable");

        var numero = $"FAC-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var facture = new Facture
        {
            DevisId = devis.Id,
            Total = devis.Total,
            Numero = numero,
            Statut = "NonPayee"
        };

        _db.Factures.Add(facture);
        await _db.SaveChangesAsync();

        return Ok(facture);
    }

    [HttpPut("{id}/payer")]
    public async Task<IActionResult> MarquerPayee(Guid id)
    {
        var facture = await _db.Factures.FindAsync(id);
        if (facture == null) return NotFound();

        facture.Statut = "Payee";
        await _db.SaveChangesAsync();

        return Ok(facture);
    }

    // 🔥 NOUVEAU — Télécharger PDF
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var facture = await _db.Factures
            .Include(f => f.Devis)
                .ThenInclude(d => d.Client)
            .Include(f => f.Devis)
                .ThenInclude(d => d.Items)
                    .ThenInclude(i => i.ServiceItem)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (facture == null) return NotFound();

        var document = new FacturePdfDocument(facture);
        var pdfBytes = document.GeneratePdf();

        return File(pdfBytes, "application/pdf", $"facture-{facture.Numero}.pdf");
    }
}

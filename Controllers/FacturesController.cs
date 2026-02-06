using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Factures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/factures")]
[Authorize]
public class FacturesController : ControllerBase
{
    private readonly IFactureService _factures;

    public FacturesController(IFactureService factures)
    {
        _factures = factures;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFactureRequest request)
    {
        return await CreateFromDevis(request.DevisId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) return BadRequest("page doit être >= 1");
        if (pageSize < 1) return BadRequest("pageSize doit être >= 1");
        if (pageSize > 100) pageSize = 100;

        var result = await _factures.GetFactures(page, pageSize);
        return Ok(result);
    }

    [HttpPost("from-devis/{devisId}")]
    public async Task<IActionResult> CreateFromDevis(Guid devisId)
    {
        var (ok, error, facture) = await _factures.CreateFromDevis(devisId);
        if (ok && facture != null) return Ok(facture);

        if (string.Equals(error, "Devis introuvable", StringComparison.OrdinalIgnoreCase))
            return NotFound(error);

        return BadRequest(error ?? "Erreur lors de la création de la facture");
    }

    [HttpPut("{id}/payer")]
    public async Task<IActionResult> MarquerPayee(Guid id)
    {
        var facture = await _factures.MarkPaid(id);
        if (facture == null) return NotFound();
        return Ok(facture);
    }

    // 🔥 NOUVEAU — Télécharger PDF
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        var (ok, pdfBytes, fileName) = await _factures.GetFacturePdf(id);
        if (!ok || pdfBytes == null || string.IsNullOrWhiteSpace(fileName)) return NotFound();

        return File(pdfBytes, "application/pdf", fileName);
    }
}

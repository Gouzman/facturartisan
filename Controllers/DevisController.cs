using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Devis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/devis")]
[Authorize]
public class DevisController : ControllerBase
{
    private readonly IDevisService _devis;

    public DevisController(IDevisService devis)
    {
        _devis = devis;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) return BadRequest("page doit être >= 1");
        if (pageSize < 1) return BadRequest("pageSize doit être >= 1");
        if (pageSize > 100) pageSize = 100;

        var result = await _devis.GetDevis(page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDevisRequest request)
    {
        var (ok, error, devis) = await _devis.CreateDevis(request);
        if (ok && devis != null) return Ok(devis);

        if (string.Equals(error, "Client introuvable", StringComparison.OrdinalIgnoreCase))
            return NotFound(error);

        return BadRequest(error ?? "Erreur lors de la création du devis");
    }
}

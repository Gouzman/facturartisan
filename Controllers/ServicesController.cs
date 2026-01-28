using FacturArtisan.Api.Data;
using FacturArtisan.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _db.Services
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return Ok(services);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceItem service)
    {
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return Ok(service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, ServiceItem input)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return NotFound();

        service.Nom = input.Nom;
        service.Prix = input.Prix;

        await _db.SaveChangesAsync();
        return Ok(service);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return NotFound();

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

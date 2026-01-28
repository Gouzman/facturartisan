using FacturArtisan.Api.Data;
using FacturArtisan.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _db.Clients.ToListAsync();
        return Ok(clients);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Client client)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return Ok(client);
    }
}

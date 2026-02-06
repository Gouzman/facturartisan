using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clients;

    public ClientsController(IClientService clients)
    {
        _clients = clients;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) return BadRequest("page doit être >= 1");
        if (pageSize < 1) return BadRequest("pageSize doit être >= 1");
        if (pageSize > 100) pageSize = 100;

        var result = await _clients.GetClients(page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = await _clients.CreateClient(request);
        return Ok(client);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var updated = await _clients.UpdateClient(id, request);
        if (updated == null) return NotFound();
        return Ok(updated);
    }
}

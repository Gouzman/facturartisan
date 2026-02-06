using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/services")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceCatalogService _services;

    public ServicesController(IServiceCatalogService services)
    {
        _services = services;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) return BadRequest("page doit être >= 1");
        if (pageSize < 1) return BadRequest("pageSize doit être >= 1");
        if (pageSize > 100) pageSize = 100;

        var result = await _services.GetServices(page, pageSize);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequest request)
    {
        var service = await _services.CreateService(request);
        return Ok(service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateServiceRequest input)
    {
        var updated = await _services.UpdateService(id, input);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _services.DeleteService(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}

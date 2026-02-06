using FacturArtisan.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacturArtisan.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var stats = await _dashboard.GetMonthlyStatsUtc();
            return Ok(stats);
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

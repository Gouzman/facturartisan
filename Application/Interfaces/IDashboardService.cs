using FacturArtisan.Api.DTOs;

namespace FacturArtisan.Api.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetMonthlyStatsUtc();
}

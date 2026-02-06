using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Application.DTOs.Devis;

namespace FacturArtisan.Api.Application.Interfaces;

public interface IDevisService
{
    Task<PagedResult<DevisDto>> GetDevis(int page, int pageSize);
    Task<(bool ok, string? error, DevisDto? devis)> CreateDevis(CreateDevisRequest request);
}

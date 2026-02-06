using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Application.DTOs.Factures;

namespace FacturArtisan.Api.Application.Interfaces;

public interface IFactureService
{
    Task<PagedResult<FactureDto>> GetFactures(int page, int pageSize);
    Task<(bool ok, string? error, FactureDto? facture)> CreateFromDevis(Guid devisId);
    Task<FactureDto?> MarkPaid(Guid id);
    Task<(bool ok, byte[]? pdfBytes, string? fileName)> GetFacturePdf(Guid id);
}

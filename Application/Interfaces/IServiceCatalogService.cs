using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Application.DTOs.Services;

namespace FacturArtisan.Api.Application.Interfaces;

public interface IServiceCatalogService
{
    Task<PagedResult<ServiceDto>> GetServices(int page, int pageSize);
    Task<ServiceDto> CreateService(CreateServiceRequest request);
    Task<ServiceDto?> UpdateService(Guid id, CreateServiceRequest request);
    Task<bool> DeleteService(Guid id);
}

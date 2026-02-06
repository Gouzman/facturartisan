using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Services;
using FacturArtisan.Api.Data;
using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Application.Services;

public class ServiceCatalogService : IServiceCatalogService
{
    private readonly AppDbContext _db;

    public ServiceCatalogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ServiceDto>> GetServices(int page, int pageSize)
    {
        var baseQuery = _db.Services.AsNoTracking().OrderByDescending(s => s.CreatedAt);
        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Nom = s.Nom,
                Prix = s.Prix,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<ServiceDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<ServiceDto> CreateService(CreateServiceRequest request)
    {
        var service = new ServiceItem
        {
            Nom = request.Nom.Trim(),
            Prix = request.Prix
        };

        _db.Services.Add(service);
        await _db.SaveChangesAsync();

        return new ServiceDto
        {
            Id = service.Id,
            Nom = service.Nom,
            Prix = service.Prix,
            CreatedAt = service.CreatedAt
        };
    }

    public async Task<ServiceDto?> UpdateService(Guid id, CreateServiceRequest request)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return null;

        service.Nom = request.Nom.Trim();
        service.Prix = request.Prix;

        await _db.SaveChangesAsync();

        return new ServiceDto
        {
            Id = service.Id,
            Nom = service.Nom,
            Prix = service.Prix,
            CreatedAt = service.CreatedAt
        };
    }

    public async Task<bool> DeleteService(Guid id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return false;

        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return true;
    }
}

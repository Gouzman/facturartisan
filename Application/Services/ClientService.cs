using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Data;
using FacturArtisan.Api.Application.DTOs.Clients;
using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Models;
using Microsoft.EntityFrameworkCore;
using ClientDto = FacturArtisan.Api.Application.DTOs.Clients.ClientDto;

namespace FacturArtisan.Api.Application.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;

    public ClientService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ClientDto>> GetClients(int page, int pageSize)
    {
        var baseQuery = _db.Clients.AsNoTracking().OrderByDescending(c => c.CreatedAt);
        var totalCount = await baseQuery.CountAsync();

        var items = await baseQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClientDto
            {
                Id = c.Id,
                Nom = c.Nom,
                Telephone = c.Telephone,
                Type = c.Type,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<ClientDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<ClientDto> CreateClient(CreateClientRequest request)
    {
        var client = new Client
        {
            Nom = request.Nom.Trim(),
            Telephone = request.Telephone.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Particulier" : request.Type.Trim()
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return new ClientDto
        {
            Id = client.Id,
            Nom = client.Nom,
            Telephone = client.Telephone,
            Type = client.Type,
            CreatedAt = client.CreatedAt
        };
    }

    public async Task<ClientDto?> UpdateClient(Guid id, UpdateClientRequest request)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return null;

        client.Nom = request.Nom.Trim();
        client.Telephone = request.Telephone.Trim();
        client.Type = string.IsNullOrWhiteSpace(request.Type) ? "Particulier" : request.Type.Trim();

        await _db.SaveChangesAsync();

        return new ClientDto
        {
            Id = client.Id,
            Nom = client.Nom,
            Telephone = client.Telephone,
            Type = client.Type,
            CreatedAt = client.CreatedAt
        };
    }
}

using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Devis;
using FacturArtisan.Api.Data;
using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FacturArtisan.Api.Application.Services;

public class DevisService : IDevisService
{
    private readonly AppDbContext _db;

    public DevisService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<DevisDto>> GetDevis(int page, int pageSize)
    {
        var totalCount = await _db.Devis.AsNoTracking().CountAsync();

        var items = await _db.Devis
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DevisDto
            {
                Id = d.Id,
                ClientId = d.ClientId,
                ClientNom = d.Client.Nom,
                Total = d.Total,
                Statut = d.Statut,
                CreatedAt = d.CreatedAt,
                Items = d.Items.Select(i => new DevisItemDto
                {
                    Id = i.Id,
                    ServiceItemId = i.ServiceItemId,
                    ServiceNom = i.ServiceItem.Nom,
                    Quantite = i.Quantite,
                    PrixUnitaire = i.PrixUnitaire,
                    Total = i.Total
                }).ToList()
            })
            .ToListAsync();

        return new PagedResult<DevisDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<(bool ok, string? error, DevisDto? devis)> CreateDevis(CreateDevisRequest request)
    {
        var clientExists = await _db.Clients.AsNoTracking().AnyAsync(c => c.Id == request.ClientId);
        if (!clientExists) return (false, "Client introuvable", null);

        var serviceIds = request.Items.Select(i => i.ServiceItemId).Distinct().ToList();
        var existingServices = await _db.Services
            .AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var missing = serviceIds.Except(existingServices).ToList();
        if (missing.Count > 0) return (false, "Un ou plusieurs services sont introuvables", null);

        var devis = new Devis
        {
            ClientId = request.ClientId,
            Items = request.Items.Select(i => new DevisItem
            {
                ServiceItemId = i.ServiceItemId,
                Quantite = i.Quantite,
                PrixUnitaire = i.PrixUnitaire,
                Total = i.Quantite * i.PrixUnitaire
            }).ToList()
        };

        devis.Total = devis.Items.Sum(i => i.Total);

        _db.Devis.Add(devis);
        await _db.SaveChangesAsync();

        var dto = await _db.Devis
            .AsNoTracking()
            .Where(d => d.Id == devis.Id)
            .Select(d => new DevisDto
            {
                Id = d.Id,
                ClientId = d.ClientId,
                ClientNom = d.Client.Nom,
                Total = d.Total,
                Statut = d.Statut,
                CreatedAt = d.CreatedAt,
                Items = d.Items.Select(i => new DevisItemDto
                {
                    Id = i.Id,
                    ServiceItemId = i.ServiceItemId,
                    ServiceNom = i.ServiceItem.Nom,
                    Quantite = i.Quantite,
                    PrixUnitaire = i.PrixUnitaire,
                    Total = i.Total
                }).ToList()
            })
            .FirstAsync();

        return (true, null, dto);
    }
}

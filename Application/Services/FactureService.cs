using FacturArtisan.Api.Application.Interfaces;
using FacturArtisan.Api.Application.DTOs.Factures;
using FacturArtisan.Api.Data;
using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Models;
using FacturArtisan.Api.Pdf;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace FacturArtisan.Api.Application.Services;

public class FactureService : IFactureService
{
    private readonly AppDbContext _db;

    public FactureService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<FactureDto>> GetFactures(int page, int pageSize)
    {
        var totalCount = await _db.Factures.AsNoTracking().CountAsync();

        var items = await _db.Factures
            .AsNoTracking()
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FactureDto
            {
                Id = f.Id,
                DevisId = f.DevisId,
                Numero = f.Numero,
                Total = f.Total,
                Statut = f.Statut,
                CreatedAt = f.CreatedAt,
                ClientId = f.Devis.ClientId,
                ClientNom = f.Devis.Client.Nom
            })
            .ToListAsync();

        return new PagedResult<FactureDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<(bool ok, string? error, FactureDto? facture)> CreateFromDevis(Guid devisId)
    {
        var devis = await _db.Devis
            .AsNoTracking()
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == devisId);

        if (devis == null)
            return (false, "Devis introuvable", null);

        var numero = $"FAC-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var facture = new Facture
        {
            DevisId = devis.Id,
            Total = devis.Total,
            Numero = numero,
            Statut = "NonPayee"
        };

        _db.Factures.Add(facture);
        await _db.SaveChangesAsync();

        var dto = await _db.Factures
            .AsNoTracking()
            .Where(f => f.Id == facture.Id)
            .Select(f => new FactureDto
            {
                Id = f.Id,
                DevisId = f.DevisId,
                Numero = f.Numero,
                Total = f.Total,
                Statut = f.Statut,
                CreatedAt = f.CreatedAt,
                ClientId = f.Devis.ClientId,
                ClientNom = f.Devis.Client.Nom
            })
            .FirstAsync();

        return (true, null, dto);
    }

    public async Task<FactureDto?> MarkPaid(Guid id)
    {
        var facture = await _db.Factures.FindAsync(id);
        if (facture == null) return null;

        facture.Statut = "Payee";
        await _db.SaveChangesAsync();

        return await _db.Factures
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FactureDto
            {
                Id = f.Id,
                DevisId = f.DevisId,
                Numero = f.Numero,
                Total = f.Total,
                Statut = f.Statut,
                CreatedAt = f.CreatedAt,
                ClientId = f.Devis.ClientId,
                ClientNom = f.Devis.Client.Nom
            })
            .FirstAsync();
    }

    public async Task<(bool ok, byte[]? pdfBytes, string? fileName)> GetFacturePdf(Guid id)
    {
        var facture = await _db.Factures
            .AsNoTracking()
            .Include(f => f.Devis)
                .ThenInclude(d => d.Client)
            .Include(f => f.Devis)
                .ThenInclude(d => d.Items)
                    .ThenInclude(i => i.ServiceItem)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (facture == null) return (false, null, null);

        var document = new FacturePdfDocument(facture);
        var pdfBytes = document.GeneratePdf();
        return (true, pdfBytes, $"facture-{facture.Numero}.pdf");
    }
}

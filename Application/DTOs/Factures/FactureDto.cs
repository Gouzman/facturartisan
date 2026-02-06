namespace FacturArtisan.Api.Application.DTOs.Factures;

public class FactureDto
{
    public Guid Id { get; set; }
    public Guid DevisId { get; set; }

    public string Numero { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Guid ClientId { get; set; }
    public string ClientNom { get; set; } = string.Empty;
}

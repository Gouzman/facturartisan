namespace FacturArtisan.Api.Application.DTOs.Devis;

public class DevisDto
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public string ClientNom { get; set; } = string.Empty;

    public decimal Total { get; set; }
    public string Statut { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public List<DevisItemDto> Items { get; set; } = new();
}

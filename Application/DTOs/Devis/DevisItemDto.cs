namespace FacturArtisan.Api.Application.DTOs.Devis;

public class DevisItemDto
{
    public Guid Id { get; set; }
    public Guid ServiceItemId { get; set; }
    public string ServiceNom { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Total { get; set; }
}

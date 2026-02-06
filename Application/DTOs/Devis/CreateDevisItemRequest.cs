namespace FacturArtisan.Api.Application.DTOs.Devis;

public class CreateDevisItemRequest
{
    public Guid ServiceItemId { get; set; }
    public int Quantite { get; set; } = 1;
    public decimal PrixUnitaire { get; set; }
}

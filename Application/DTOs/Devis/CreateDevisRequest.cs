namespace FacturArtisan.Api.Application.DTOs.Devis;

public class CreateDevisRequest
{
    public Guid ClientId { get; set; }
    public List<CreateDevisItemRequest> Items { get; set; } = new();
}

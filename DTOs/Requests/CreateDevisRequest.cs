namespace FacturArtisan.Api.DTOs.Requests;

public class CreateDevisRequest
{
    public Guid ClientId { get; set; }
    public List<CreateDevisItemRequest> Items { get; set; } = new();
}

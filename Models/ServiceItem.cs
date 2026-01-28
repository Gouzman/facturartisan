namespace FacturArtisan.Api.Models;

public class ServiceItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public decimal Prix { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace FacturArtisan.Api.Models;

public class Client
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Type { get; set; } = "Particulier";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

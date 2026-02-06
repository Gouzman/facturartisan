namespace FacturArtisan.Api.Application.DTOs.Services;

public class ServiceDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public decimal Prix { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace FacturArtisan.Api.Application.DTOs.Services;

public class CreateServiceRequest
{
    public string Nom { get; set; } = string.Empty;
    public decimal Prix { get; set; }
}

namespace FacturArtisan.Api.DTOs.Requests;

public class CreateServiceRequest
{
    public string Nom { get; set; } = string.Empty;
    public decimal Prix { get; set; }
}

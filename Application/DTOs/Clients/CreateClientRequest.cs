namespace FacturArtisan.Api.Application.DTOs.Clients;

public class CreateClientRequest
{
    public string Nom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Type { get; set; } = "Particulier";
}

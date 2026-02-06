namespace FacturArtisan.Api.DTOs.Requests;

public class UpdateClientRequest
{
    public string Nom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Type { get; set; } = "Particulier";
}

namespace FacturArtisan.Api.Application.DTOs.Clients;

public class ClientDto
{
    public Guid Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string Type { get; set; } = "Particulier";
    public DateTime CreatedAt { get; set; }
}

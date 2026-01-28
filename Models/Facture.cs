namespace FacturArtisan.Api.Models;

public class Facture
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DevisId { get; set; }
    public Devis Devis { get; set; } = null!;

    public string Numero { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Statut { get; set; } = "NonPayee"; // NonPayee, Payee

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

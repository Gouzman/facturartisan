namespace FacturArtisan.Api.Models;

public class Devis
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public decimal Total { get; set; }
    public string Statut { get; set; } = "Brouillon"; // Brouillon, Envoye, Accepte, Refuse
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<DevisItem> Items { get; set; } = new();
}

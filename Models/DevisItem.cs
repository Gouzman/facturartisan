namespace FacturArtisan.Api.Models;

public class DevisItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DevisId { get; set; }
    public Devis Devis { get; set; } = null!;

    public Guid ServiceItemId { get; set; }
    public ServiceItem ServiceItem { get; set; } = null!;

    public int Quantite { get; set; } = 1;
    public decimal PrixUnitaire { get; set; }
    public decimal Total { get; set; }
}

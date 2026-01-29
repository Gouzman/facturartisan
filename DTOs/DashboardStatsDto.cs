namespace FacturArtisan.Api.DTOs;

public class DashboardStatsDto
{
    public decimal TotalMois { get; set; }
    public decimal TotalEncaisse { get; set; }
    public decimal TotalEnAttente { get; set; }
    public int NombreFactures { get; set; }
}

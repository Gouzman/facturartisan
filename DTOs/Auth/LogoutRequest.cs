namespace FacturArtisan.Api.DTOs.Auth;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

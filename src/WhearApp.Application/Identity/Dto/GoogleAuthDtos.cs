namespace WhearApp.Application.Identity.Dto;

public class GoogleTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
}

public class GoogleUserInfo
{
    public string Sub { get; set; } = string.Empty; // Google ID
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
}
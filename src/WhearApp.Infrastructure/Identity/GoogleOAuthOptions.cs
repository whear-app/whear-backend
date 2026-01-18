namespace WhearApp.Infrastructure.Identity;

public sealed class GoogleOAuthOptions
{
    public string ClientId { get; init; } = default!;
    public string ClientSecret { get; init; } = default!;
    public string RedirectUri { get; init; } = default!;
}

using WhearApp.Core.Identity;

namespace WhearApp.Application.Identity.Repositories;

public interface IIdentityRepository
{
    Task<RefreshTokenEntity?> GetRefreshTokenAsync(string refreshToken);
    Task<bool> AddRefreshTokenAsync(RefreshTokenEntity refreshToken, Guid userId);
}
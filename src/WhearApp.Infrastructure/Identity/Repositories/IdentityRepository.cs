using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WhearApp.Application.Identity.Repositories;
using WhearApp.Core.Identity;
using WhearApp.Infrastructure.Database;

namespace WhearApp.Infrastructure.Identity.Repositories;

public class IdentityRepository(ApplicationDbContext dbContext, ILogger<IdentityRepository> logger)
    : IIdentityRepository
{

    public async Task<RefreshTokenEntity?> GetRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var token = await dbContext.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);
            return token;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error retrieving refresh token");
            throw;
        }
    }

    public async Task<bool> AddRefreshTokenAsync(RefreshTokenEntity refreshToken, Guid userId)
    {
        try
        {
            var oldTokens = await dbContext.RefreshTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip(5)
                .ToListAsync();
            
            dbContext.RefreshTokens.RemoveRange(oldTokens);
            await dbContext.RefreshTokens.AddAsync(refreshToken);
            var rs = await dbContext.SaveChangesAsync();
            return rs > 0;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error adding refresh token");
            throw;
        }
    }

}
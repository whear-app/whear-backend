using Microsoft.AspNetCore.Identity;
using WhearApp.BuildingBlocks.SharedKernel.Common;

namespace WhearApp.Core.Identity;

public class UserEntity : IdentityUser<Guid>, IEntityBase<Guid>
{
    
}
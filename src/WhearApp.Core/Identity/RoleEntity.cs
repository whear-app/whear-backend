using Microsoft.AspNetCore.Identity;
using WhearApp.BuildingBlocks.SharedKernel.Common;

namespace WhearApp.Core.Identity;

public class RoleEntity : IdentityRole<Guid>, IEntityBase<Guid>
{
    
}
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace WhearApp.Infrastructure.Database.Generators;

public class GuidVersion7Generator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry)
    {
        return IdGenHelper.NewGuidId();
    }
}

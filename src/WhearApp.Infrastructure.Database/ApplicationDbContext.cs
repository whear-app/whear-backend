using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WhearApp.BuildingBlocks.SharedKernel.Common;

namespace WhearApp.Infrastructure.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        
        // Global soft-delete filter
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var p = Expression.Parameter(entityType.ClrType, "e");
                var prop = Expression.Property(p, nameof(ISoftDelete.IsDeleted));
                var cond = Expression.Equal(prop, Expression.Constant(false));
                var lambda = Expression.Lambda(cond, p);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }

        ConfigureAuditFields(modelBuilder);
    }
    
    private static void ConfigureAuditFields(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            if (typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditableEntity.CreatedAt))
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAdd();

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IAuditableEntity.UpdatedAt))
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            }
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry is { State: EntityState.Added, Entity: IEntityBase<Guid> gen } &&
                gen.Id == Guid.Empty)
                gen.Id = IdGenHelper.NewGuidId();

            if (entry.Entity is IAuditableEntity aud)
                switch (entry.State)
                {
                    case EntityState.Added:
                        aud.CreatedAt = utcNow;
                        aud.UpdatedAt = utcNow;
                        break;
                    case EntityState.Modified:
                        aud.UpdatedAt = utcNow;
                        entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                        break;
                    case EntityState.Detached:
                    case EntityState.Unchanged:
                    case EntityState.Deleted:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            if (entry is { Entity: ISoftDelete soft, State: EntityState.Deleted })
            {
                entry.State = EntityState.Modified;
                soft.IsDeleted = true;
                soft.DeletedAt = utcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
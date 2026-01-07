using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhearApp.BuildingBlocks.SharedKernel.Common;
using WhearApp.Core.Identity;

namespace WhearApp.Infrastructure.Database;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : 
    IdentityDbContext<UserEntity, RoleEntity, Guid>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
        });

        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.ToTable("roles");
        });

        modelBuilder.Entity<IdentityUserRole<Guid>>(entity =>
        {
            entity.ToTable("user_roles");
        });

        modelBuilder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
        });

        modelBuilder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
        });

        modelBuilder.Entity<IdentityRoleClaim<Guid>>(entity =>
        {
            entity.ToTable("role_claims");
        });

        modelBuilder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedByIp).HasMaxLength(45);
            entity.Property(e => e.RevokedByIp).HasMaxLength(45);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token);
            entity.HasIndex(e => e.UserId);
        });
        
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
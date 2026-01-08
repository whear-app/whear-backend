using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using WhearApp.Application.Identity.Abstractions;
using WhearApp.Core.Identity;
using WhearApp.Infrastructure.Database;
using WhearApp.Infrastructure.Identity.Security;
using WhearApp.Infrastructure.Identity.Services;

namespace WhearApp.WebApi.Extensions.DI;

public static class DatabaseServiceExtensions
{
    
    public static void AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IKeyManagementService, KeyManagementService>();

        // Register Identity application services
        services.AddScoped<IAuthService, AuthService>();

        services.AddIdentity<UserEntity, RoleEntity>(options =>
            {
                // password
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidAudience = jwtSettings?.Audience,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                    {
                        var keyService = services.BuildServiceProvider()
                            .GetRequiredService<IKeyManagementService>();
                        return keyService.GetAllPublicKeys();
                    }
                };
            });
    }
    
    public static void AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment
    )
    {
        var databaseSection = configuration.GetSection(DatabaseOptions.SectionName);
        services.Configure<DatabaseOptions>(databaseSection);

        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            var dbOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = BuildConnectionString(dbOptions);

            options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        3,
                        TimeSpan.FromSeconds(10),
                        null);

                    npgsqlOptions.CommandTimeout(30);
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                })
                .UseSnakeCaseNamingConvention();

            if (!environment.IsDevelopment()) return;
            options.EnableSensitiveDataLogging(dbOptions.EnableSensitiveDataLogging);
            options.EnableDetailedErrors(dbOptions.EnableDetailedErrors);
        });
    }
    
    private static string BuildConnectionString(DatabaseOptions dbOptions)
    {
        var builder = new NpgsqlConnectionStringBuilder(dbOptions.ConnectionString)
        {
            MinPoolSize = dbOptions.ConnectionPool.MinPoolSize,
            MaxPoolSize = dbOptions.ConnectionPool.MaxPoolSize,
            ConnectionIdleLifetime = dbOptions.ConnectionPool.ConnectionIdleLifetime,
            Timeout = dbOptions.ConnectionPool.ConnectionTimeout,
            Multiplexing = false,
            TcpKeepAlive = true,
            TcpKeepAliveTime = 600,
            TcpKeepAliveInterval = 30,
            ApplicationName = "WebAPI",
            MaxAutoPrepare = 0
        };

        return builder.ConnectionString;
    }
}
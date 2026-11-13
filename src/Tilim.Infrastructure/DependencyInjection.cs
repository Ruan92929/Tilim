using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tilim.Application.Abstractions;
using Tilim.Infrastructure.Persistence;
using Tilim.Infrastructure.Pix;
using Tilim.Infrastructure.QrCodes;

namespace Tilim.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Wires every Infrastructure concern behind the Application layer's interfaces.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default não configurada (appsettings.json ou variável de ambiente).");

        // "Database:Provider": "Sqlite" runs with zero setup (local .db file);
        // anything else uses the real Postgres provider.
        var useSqlite = string.Equals(
            configuration["Database:Provider"], "Sqlite", StringComparison.OrdinalIgnoreCase);

        services.AddDbContext<TilimDbContext>(options =>
        {
            if (useSqlite)
                options.UseSqlite(connectionString);
            else
                options.UseNpgsql(connectionString);
        });

        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddSingleton<IPixPayloadBuilder, BrCodePayloadBuilder>();
        services.AddSingleton<IQrCodeImageGenerator, QrCoderPngGenerator>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    /// <summary>
    /// Dev-only: creates the SQLite file and schema straight from the EF Core
    /// model. Never use against Postgres/production — real deployments need
    /// actual EF Core migrations, which this project doesn't have yet.
    /// </summary>
    public static void EnsureDevelopmentDatabaseCreated(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TilimDbContext>();
        db.Database.EnsureCreated();
    }
}

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

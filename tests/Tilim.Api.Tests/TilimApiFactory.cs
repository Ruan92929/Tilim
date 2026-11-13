using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tilim.Infrastructure.Persistence;

namespace Tilim.Api.Tests;

/// <summary>
/// Boots the real Tilim.Api pipeline (routing, output caching, rate
/// limiting, the real WorkerService, the real BrCodePayloadBuilder) but
/// swaps PostgreSQL for an in-memory SQLite database, so these tests need
/// no running database — only `dotnet test`. The open SqliteConnection is
/// kept alive for the factory's lifetime because an in-memory SQLite
/// database is destroyed the moment its one connection closes.
/// </summary>
public sealed class TilimApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:;Cache=Shared");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            // Removing DbContextOptions<TilimDbContext> alone isn't enough:
            // since EF Core 9, AddDbContext also registers an
            // IDbContextOptionsConfiguration<TilimDbContext> entry that
            // captures the original UseNpgsql(...) configure action. If
            // that's left in place, EF Core applies BOTH the Npgsql and
            // the Sqlite configuration when it builds the options, and
            // throws "Services for database providers ... have been
            // registered" because two providers ended up configured on the
            // same context. Removing both entries is the documented fix.
            services.RemoveAll<DbContextOptions<TilimDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TilimDbContext>>();
            services.AddDbContext<TilimDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TilimDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

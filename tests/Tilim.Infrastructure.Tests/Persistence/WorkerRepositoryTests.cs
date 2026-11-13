using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tilim.Domain.Entities;
using Tilim.Domain.Enums;
using Tilim.Domain.ValueObjects;
using Tilim.Infrastructure.Persistence;

namespace Tilim.Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises the real EF Core mapping — in particular the JSON value
/// converter for SuggestedAmounts and the unique index on Slug — against
/// SQLite in-memory. This is the layer most likely to silently break on a
/// schema change, so it gets its own round-trip test instead of only being
/// covered indirectly through the API tests.
/// </summary>
public sealed class WorkerRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly TilimDbContext _db;

    public WorkerRepositoryTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<TilimDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new TilimDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Worker NewWorker(string slug) => Worker.Create(
        slug, "Pedro, entregador", "pedro@example.com", PixKeyType.Email,
        "Curitiba", [4m, 8m, 20m], "Valeu demais!", DateTimeOffset.UtcNow);

    [Fact]
    public async Task AddAndGetBySlug_RoundTripsSuggestedAmounts()
    {
        var repository = new WorkerRepository(_db);
        await repository.AddAsync(NewWorker("pedro-entregador"));
        await repository.SaveChangesAsync();

        // New DbContext instance so the result comes from the database,
        // not EF Core's change-tracker cache.
        await using var freshDb = new TilimDbContext(
            new DbContextOptionsBuilder<TilimDbContext>().UseSqlite(_connection).Options);
        var fresh = new WorkerRepository(freshDb);

        var loaded = await fresh.GetBySlugAsync(Slug.Create("pedro-entregador"));

        Assert.NotNull(loaded);
        Assert.Equal([4m, 8m, 20m], loaded!.SuggestedAmounts);
        Assert.Equal("pedro@example.com", loaded.PixKey);
    }

    [Fact]
    public async Task SlugExistsAsync_IsCaseInsensitive()
    {
        var repository = new WorkerRepository(_db);
        await repository.AddAsync(NewWorker("ana-fotografa"));
        await repository.SaveChangesAsync();

        Assert.True(await repository.SlugExistsAsync(Slug.Create("ANA-FOTOGRAFA")));
    }

    [Fact]
    public async Task AddingTwoWorkersWithSameSlug_ViolatesUniqueIndex()
    {
        var repository = new WorkerRepository(_db);
        await repository.AddAsync(NewWorker("carlos-musico"));
        await repository.SaveChangesAsync();

        await repository.AddAsync(NewWorker("carlos-musico"));
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => repository.SaveChangesAsync());
    }
}

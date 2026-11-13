using Microsoft.EntityFrameworkCore;
using Tilim.Application.Abstractions;
using Tilim.Domain.Entities;
using Tilim.Domain.ValueObjects;

namespace Tilim.Infrastructure.Persistence;

public sealed class WorkerRepository : IWorkerRepository
{
    private readonly TilimDbContext _db;

    public WorkerRepository(TilimDbContext db) => _db = db;

    public Task<Worker?> GetBySlugAsync(Slug slug, CancellationToken ct = default) =>
        _db.Workers.SingleOrDefaultAsync(w => w.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(Slug slug, CancellationToken ct = default) =>
        _db.Workers.AnyAsync(w => w.Slug == slug, ct);

    public Task AddAsync(Worker worker, CancellationToken ct = default)
    {
        _db.Workers.Add(worker);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}

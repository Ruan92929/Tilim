using Tilim.Domain.Entities;
using Tilim.Domain.ValueObjects;

namespace Tilim.Application.Abstractions;

/// <summary>
/// Persistence port for Worker. The Application layer only knows this
/// interface — Tilim.Infrastructure provides the EF Core / PostgreSQL
/// implementation, which keeps the business rules in Worker testable
/// without a database.
/// </summary>
public interface IWorkerRepository
{
    Task<Worker?> GetBySlugAsync(Slug slug, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(Slug slug, CancellationToken ct = default);

    Task AddAsync(Worker worker, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

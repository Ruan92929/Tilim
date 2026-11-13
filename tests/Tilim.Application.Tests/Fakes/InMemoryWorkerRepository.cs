using Tilim.Application.Abstractions;
using Tilim.Domain.Entities;
using Tilim.Domain.ValueObjects;

namespace Tilim.Application.Tests.Fakes;

/// <summary>
/// A real (not mocked) in-memory implementation of IWorkerRepository.
/// Deliberately hand-rolled instead of reaching for a mocking library: it
/// behaves like a real repository (SlugExistsAsync actually looks at what
/// was added), which catches bugs a loose mock would miss, and it keeps
/// the test project dependency-free.
/// </summary>
public sealed class InMemoryWorkerRepository : IWorkerRepository
{
    private readonly List<Worker> _workers = [];

    public IReadOnlyList<Worker> Workers => _workers;

    public Task<Worker?> GetBySlugAsync(Slug slug, CancellationToken ct = default) =>
        Task.FromResult(_workers.SingleOrDefault(w => w.Slug.Equals(slug)));

    public Task<bool> SlugExistsAsync(Slug slug, CancellationToken ct = default) =>
        Task.FromResult(_workers.Any(w => w.Slug.Equals(slug)));

    public Task AddAsync(Worker worker, CancellationToken ct = default)
    {
        _workers.Add(worker);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

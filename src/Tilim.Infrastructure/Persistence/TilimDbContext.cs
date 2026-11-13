using Microsoft.EntityFrameworkCore;
using Tilim.Domain.Entities;
using Tilim.Infrastructure.Persistence.Configurations;

namespace Tilim.Infrastructure.Persistence;

public sealed class TilimDbContext : DbContext
{
    public TilimDbContext(DbContextOptions<TilimDbContext> options) : base(options)
    {
    }

    public DbSet<Worker> Workers => Set<Worker>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WorkerConfiguration());
    }
}

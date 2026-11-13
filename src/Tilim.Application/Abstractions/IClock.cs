namespace Tilim.Application.Abstractions;

/// <summary>
/// Thin wrapper around "now", so tests can control time instead of racing
/// against DateTimeOffset.UtcNow.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

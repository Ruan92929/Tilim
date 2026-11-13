namespace Tilim.Domain.Exceptions;

/// <summary>
/// Raised when a domain invariant is violated (invalid state that must never
/// be allowed to exist, regardless of which layer is calling in).
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

using System.Text.RegularExpressions;
using Tilim.Domain.Exceptions;

namespace Tilim.Domain.ValueObjects;

/// <summary>
/// The short, URL-safe identifier a worker is reachable at: tilim.app/w/&lt;slug&gt;.
/// Normalized to lowercase so "Joao.Garcom" and "joao-garcom" never collide ambiguously.
/// </summary>
public sealed partial class Slug : IEquatable<Slug>
{
    public const int MinLength = 3;
    public const int MaxLength = 32;

    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new DomainException("O identificador público (slug) é obrigatório.");

        var normalized = raw.Trim().ToLowerInvariant();

        if (normalized.Length < MinLength || normalized.Length > MaxLength)
            throw new DomainException($"O slug deve ter entre {MinLength} e {MaxLength} caracteres.");

        if (!SlugFormatRegex().IsMatch(normalized))
        {
            throw new DomainException(
                "O slug só pode conter letras minúsculas, números e hífen, " +
                "e não pode começar ou terminar com hífen.");
        }

        return new Slug(normalized);
    }

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex SlugFormatRegex();

    public bool Equals(Slug? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => Equals(obj as Slug);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}

using System.Text.RegularExpressions;
using Tilim.Domain.Enums;
using Tilim.Domain.Exceptions;
using Tilim.Domain.ValueObjects;

namespace Tilim.Domain.Entities;

/// <summary>
/// A tipped professional (garçom, manicure, músico, entregador...) who has a
/// public tipping page. Tilim never touches the money itself — the Pix QR
/// code sends the payer straight to the worker's own bank account.
/// </summary>
public sealed partial class Worker
{
    public const int MaxDisplayNameLength = 60;
    public const int MaxCityLength = 60;
    public const int MaxThankYouMessageLength = 140;
    public const int MinSuggestedAmounts = 1;
    public const int MaxSuggestedAmounts = 6;
    public const decimal MaxSuggestedAmount = 100_000m;

    public Guid Id { get; private set; }
    public Slug Slug { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public string PixKey { get; private set; } = null!;
    public PixKeyType PixKeyType { get; private set; }
    public string City { get; private set; } = null!;
    public string? ThankYouMessage { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private readonly List<decimal> _suggestedAmounts = [];
    public IReadOnlyList<decimal> SuggestedAmounts => _suggestedAmounts;

    // EF Core materializes entities without calling application code.
    private Worker()
    {
    }

    public static Worker Create(
        string slug,
        string displayName,
        string pixKey,
        PixKeyType pixKeyType,
        string city,
        IEnumerable<decimal> suggestedAmounts,
        string? thankYouMessage,
        DateTimeOffset nowUtc)
    {
        var worker = new Worker
        {
            Id = Guid.NewGuid(),
            Slug = Slug.Create(slug),
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        worker.SetDisplayName(displayName);
        worker.SetPixKey(pixKey, pixKeyType);
        worker.SetCity(city);
        worker.SetSuggestedAmounts(suggestedAmounts);
        worker.SetThankYouMessage(thankYouMessage);

        return worker;
    }

    public void UpdateProfile(
        string displayName,
        string pixKey,
        PixKeyType pixKeyType,
        string city,
        IEnumerable<decimal> suggestedAmounts,
        string? thankYouMessage,
        DateTimeOffset nowUtc)
    {
        SetDisplayName(displayName);
        SetPixKey(pixKey, pixKeyType);
        SetCity(city);
        SetSuggestedAmounts(suggestedAmounts);
        SetThankYouMessage(thankYouMessage);
        UpdatedAtUtc = nowUtc;
    }

    public void Deactivate(DateTimeOffset nowUtc)
    {
        IsActive = false;
        UpdatedAtUtc = nowUtc;
    }

    public void Activate(DateTimeOffset nowUtc)
    {
        IsActive = true;
        UpdatedAtUtc = nowUtc;
    }

    private void SetDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("O nome de exibição é obrigatório.");

        var trimmed = displayName.Trim();
        if (trimmed.Length > MaxDisplayNameLength)
            throw new DomainException($"O nome de exibição deve ter no máximo {MaxDisplayNameLength} caracteres.");

        DisplayName = trimmed;
    }

    private void SetCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("A cidade é obrigatória (ela aparece no QR Code do Pix).");

        var trimmed = city.Trim();
        if (trimmed.Length > MaxCityLength)
            throw new DomainException($"A cidade deve ter no máximo {MaxCityLength} caracteres.");

        City = trimmed;
    }

    private void SetThankYouMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            ThankYouMessage = null;
            return;
        }

        var trimmed = message.Trim();
        if (trimmed.Length > MaxThankYouMessageLength)
            throw new DomainException($"A mensagem de agradecimento deve ter no máximo {MaxThankYouMessageLength} caracteres.");

        ThankYouMessage = trimmed;
    }

    private void SetSuggestedAmounts(IEnumerable<decimal>? amounts)
    {
        var list = (amounts ?? []).ToList();

        if (list.Count < MinSuggestedAmounts || list.Count > MaxSuggestedAmounts)
        {
            throw new DomainException(
                $"Informe entre {MinSuggestedAmounts} e {MaxSuggestedAmounts} valores sugeridos de gorjeta.");
        }

        if (list.Any(a => a <= 0 || a > MaxSuggestedAmount))
        {
            throw new DomainException(
                $"Cada valor sugerido deve ser maior que zero e no máximo {MaxSuggestedAmount:0}.");
        }

        _suggestedAmounts.Clear();
        _suggestedAmounts.AddRange(list.OrderBy(a => a));
    }

    private void SetPixKey(string? pixKey, PixKeyType pixKeyType)
    {
        if (string.IsNullOrWhiteSpace(pixKey))
            throw new DomainException("A chave Pix é obrigatória.");

        var trimmed = pixKey.Trim();

        // Só valida o formato — a posse da chave é confirmada pelo DICT quando
        // o pagador escaneia o QR Code, não por este sistema.
        var isValid = pixKeyType switch
        {
            PixKeyType.Cpf => CpfRegex().IsMatch(trimmed),
            PixKeyType.Cnpj => CnpjRegex().IsMatch(trimmed),
            PixKeyType.Email => EmailRegex().IsMatch(trimmed),
            PixKeyType.Telefone => PhoneRegex().IsMatch(trimmed),
            PixKeyType.Aleatoria => RandomKeyRegex().IsMatch(trimmed),
            _ => false
        };

        if (!isValid)
        {
            throw new DomainException(
                $"A chave Pix informada não tem um formato válido para o tipo '{pixKeyType}'.");
        }

        PixKey = trimmed;
        PixKeyType = pixKeyType;
    }

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex CpfRegex();

    [GeneratedRegex(@"^\d{14}$")]
    private static partial Regex CnpjRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^\+\d{10,14}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    private static partial Regex RandomKeyRegex();
}

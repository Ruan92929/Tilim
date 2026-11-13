using Tilim.Domain.Enums;

namespace Tilim.Application.Workers;

public sealed record CreateWorkerRequest(
    string Slug,
    string DisplayName,
    string PixKey,
    PixKeyType PixKeyType,
    string City,
    IReadOnlyList<decimal> SuggestedAmounts,
    string? ThankYouMessage);

public sealed record UpdateWorkerRequest(
    string PixKey,
    PixKeyType PixKeyType,
    string DisplayName,
    string City,
    IReadOnlyList<decimal> SuggestedAmounts,
    string? ThankYouMessage);

public sealed record WorkerSummaryDto(Guid Id, string Slug, string DisplayName);

/// <summary>
/// Everything the public tipping page needs to render, plus the Pix "copia e
/// cola" string for the default (open amount) QR code.
/// </summary>
public sealed record WorkerPublicProfileDto(
    string Slug,
    string DisplayName,
    string? ThankYouMessage,
    IReadOnlyList<decimal> SuggestedAmounts,
    string PixCopyPaste);

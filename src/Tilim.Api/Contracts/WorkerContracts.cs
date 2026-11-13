using Tilim.Domain.Enums;

namespace Tilim.Api.Contracts;

/// <summary>Body of POST /api/workers.</summary>
public sealed record CreateWorkerApiRequest(
    string Slug,
    string DisplayName,
    string PixKey,
    PixKeyType PixKeyType,
    string City,
    List<decimal> SuggestedAmounts,
    string? ThankYouMessage);

public sealed record CreateWorkerApiResponse(Guid Id, string Slug, string PublicPageUrl);

public sealed record ErrorResponse(string Message);

using Microsoft.AspNetCore.Mvc;
using Tilim.Api.Contracts;
using Tilim.Application.Common;
using Tilim.Application.Workers;

namespace Tilim.Api.Endpoints;

public static class WorkerEndpoints
{
    public static RouteGroupBuilder MapWorkerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/workers", CreateWorkerAsync)
            .WithName("CreateWorker")
            .WithSummary("Cria a página pública de gorjeta de um profissional.")
            .RequireRateLimiting("create-worker");

        group.MapGet("/w/{slug}", GetPublicProfileAsync)
            .WithName("GetWorkerPublicProfile")
            .WithSummary("Dados públicos da página de gorjeta (nome, valores sugeridos, Pix copia-e-cola).")
            .CacheOutput("public-profile");

        group.MapGet("/w/{slug}/qrcode.png", GetQrCodeAsync)
            .WithName("GetWorkerQrCode")
            .WithSummary("QR Code Pix em PNG, opcionalmente para um valor fixo.")
            .CacheOutput("qr-code");

        return group;
    }

    private static async Task<IResult> CreateWorkerAsync(
        CreateWorkerApiRequest request, WorkerService service, CancellationToken ct)
    {
        var result = await service.CreateWorkerAsync(
            new CreateWorkerRequest(
                request.Slug,
                request.DisplayName,
                request.PixKey,
                request.PixKeyType,
                request.City,
                request.SuggestedAmounts,
                request.ThankYouMessage),
            ct);

        if (!result.IsSuccess)
            return ToProblem(result.Error!);

        var dto = result.Value!;
        return Results.Created(
            $"/api/w/{dto.Slug}",
            new CreateWorkerApiResponse(dto.Id, dto.Slug, $"/w/{dto.Slug}"));
    }

    private static async Task<IResult> GetPublicProfileAsync(
        string slug, WorkerService service, CancellationToken ct)
    {
        var result = await service.GetPublicProfileAsync(slug, ct);
        return result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetQrCodeAsync(
        string slug, [FromQuery] decimal? amount, WorkerService service, CancellationToken ct)
    {
        var result = await service.GetQrCodePngAsync(slug, amount, ct);
        return result.IsSuccess
            ? Results.File(result.Value!, "image/png")
            : ToProblem(result.Error!);
    }

    private static IResult ToProblem(Error error) => error.Kind switch
    {
        ErrorKind.NotFound => Results.NotFound(new ErrorResponse(error.Message)),
        ErrorKind.Conflict => Results.Conflict(new ErrorResponse(error.Message)),
        _ => Results.BadRequest(new ErrorResponse(error.Message))
    };
}

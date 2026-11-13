using Tilim.Application.Abstractions;
using Tilim.Application.Common;
using Tilim.Domain.Entities;
using Tilim.Domain.Exceptions;
using Tilim.Domain.ValueObjects;

namespace Tilim.Application.Workers;

/// <summary>Use cases for the Worker aggregate.</summary>
public sealed class WorkerService
{
    private readonly IWorkerRepository _repository;
    private readonly IPixPayloadBuilder _pixPayloadBuilder;
    private readonly IQrCodeImageGenerator _qrCodeImageGenerator;
    private readonly IClock _clock;

    public WorkerService(
        IWorkerRepository repository,
        IPixPayloadBuilder pixPayloadBuilder,
        IQrCodeImageGenerator qrCodeImageGenerator,
        IClock clock)
    {
        _repository = repository;
        _pixPayloadBuilder = pixPayloadBuilder;
        _qrCodeImageGenerator = qrCodeImageGenerator;
        _clock = clock;
    }

    public async Task<Result<WorkerSummaryDto>> CreateWorkerAsync(
        CreateWorkerRequest request, CancellationToken ct = default)
    {
        Slug slug;
        try
        {
            slug = Slug.Create(request.Slug);
        }
        catch (DomainException ex)
        {
            return Result<WorkerSummaryDto>.Failure(Error.Validation(ex.Message));
        }

        if (await _repository.SlugExistsAsync(slug, ct))
        {
            return Result<WorkerSummaryDto>.Failure(
                Error.Conflict($"O endereço '{slug}' já está em uso. Escolha outro."));
        }

        Worker worker;
        try
        {
            worker = Worker.Create(
                slug: request.Slug,
                displayName: request.DisplayName,
                pixKey: request.PixKey,
                pixKeyType: request.PixKeyType,
                city: request.City,
                suggestedAmounts: request.SuggestedAmounts,
                thankYouMessage: request.ThankYouMessage,
                nowUtc: _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return Result<WorkerSummaryDto>.Failure(Error.Validation(ex.Message));
        }

        await _repository.AddAsync(worker, ct);
        await _repository.SaveChangesAsync(ct);

        return Result<WorkerSummaryDto>.Success(
            new WorkerSummaryDto(worker.Id, worker.Slug.Value, worker.DisplayName));
    }

    public async Task<Result<WorkerPublicProfileDto>> GetPublicProfileAsync(
        string rawSlug, CancellationToken ct = default)
    {
        var worker = await FindActiveWorkerAsync(rawSlug, ct);
        if (worker is null)
            return Result<WorkerPublicProfileDto>.Failure(Error.NotFound("Página não encontrada."));

        var payload = BuildPixPayload(worker, amount: null);

        return Result<WorkerPublicProfileDto>.Success(new WorkerPublicProfileDto(
            worker.Slug.Value,
            worker.DisplayName,
            worker.ThankYouMessage,
            worker.SuggestedAmounts,
            payload));
    }

    public async Task<Result<byte[]>> GetQrCodePngAsync(
        string rawSlug, decimal? amount, CancellationToken ct = default)
    {
        var worker = await FindActiveWorkerAsync(rawSlug, ct);
        if (worker is null)
            return Result<byte[]>.Failure(Error.NotFound("Página não encontrada."));

        if (amount is <= 0)
            return Result<byte[]>.Failure(Error.Validation("O valor da gorjeta deve ser maior que zero."));

        var payload = BuildPixPayload(worker, amount);
        var png = _qrCodeImageGenerator.GeneratePng(payload);

        return Result<byte[]>.Success(png);
    }

    private async Task<Worker?> FindActiveWorkerAsync(string rawSlug, CancellationToken ct)
    {
        Slug slug;
        try
        {
            slug = Slug.Create(rawSlug);
        }
        catch (DomainException)
        {
            return null;
        }

        var worker = await _repository.GetBySlugAsync(slug, ct);
        return worker is { IsActive: true } ? worker : null;
    }

    private string BuildPixPayload(Worker worker, decimal? amount) =>
        _pixPayloadBuilder.Build(new PixPayloadRequest(
            PixKey: worker.PixKey,
            MerchantName: worker.DisplayName,
            MerchantCity: worker.City,
            Amount: amount,
            TransactionId: null));
}

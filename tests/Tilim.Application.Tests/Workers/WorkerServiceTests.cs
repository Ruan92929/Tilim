using Tilim.Application.Common;
using Tilim.Application.Tests.Fakes;
using Tilim.Application.Workers;
using Tilim.Domain.Enums;

namespace Tilim.Application.Tests.Workers;

public class WorkerServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static (WorkerService Service, InMemoryWorkerRepository Repository) CreateSut()
    {
        var repository = new InMemoryWorkerRepository();
        var service = new WorkerService(
            repository,
            new FakePixPayloadBuilder(),
            new FakeQrCodeImageGenerator(),
            new FakeClock(Now));
        return (service, repository);
    }

    private static CreateWorkerRequest ValidRequest(string slug = "joao-garcom") => new(
        slug,
        "João, garçom",
        "joao@example.com",
        PixKeyType.Email,
        "São Paulo",
        [2m, 5m, 10m],
        "Valeu!");

    [Fact]
    public async Task CreateWorkerAsync_WithNewSlug_Succeeds()
    {
        var (service, repository) = CreateSut();

        var result = await service.CreateWorkerAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Single(repository.Workers);
        Assert.Equal("joao-garcom", result.Value!.Slug);
    }

    [Fact]
    public async Task CreateWorkerAsync_WithDuplicateSlug_ReturnsConflict()
    {
        var (service, _) = CreateSut();
        await service.CreateWorkerAsync(ValidRequest());

        var result = await service.CreateWorkerAsync(ValidRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task CreateWorkerAsync_WithInvalidSlug_ReturnsValidationError()
    {
        var (service, _) = CreateSut();

        var result = await service.CreateWorkerAsync(ValidRequest(slug: "a"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task CreateWorkerAsync_WithInvalidDomainData_ReturnsValidationError_NotException()
    {
        var (service, _) = CreateSut();
        var invalidRequest = ValidRequest() with { SuggestedAmounts = [] };

        var result = await service.CreateWorkerAsync(invalidRequest);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task GetPublicProfileAsync_ForExistingActiveWorker_ReturnsProfile()
    {
        var (service, _) = CreateSut();
        await service.CreateWorkerAsync(ValidRequest());

        var result = await service.GetPublicProfileAsync("joao-garcom");

        Assert.True(result.IsSuccess);
        Assert.Equal("João, garçom", result.Value!.DisplayName);
        Assert.Equal([2m, 5m, 10m], result.Value.SuggestedAmounts);
        Assert.Contains("FAKE-PAYLOAD", result.Value.PixCopyPaste);
    }

    [Fact]
    public async Task GetPublicProfileAsync_ForUnknownSlug_ReturnsNotFound()
    {
        var (service, _) = CreateSut();

        var result = await service.GetPublicProfileAsync("nao-existe");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task GetPublicProfileAsync_ForInactiveWorker_ReturnsNotFound()
    {
        var repository = new InMemoryWorkerRepository();
        var service = new WorkerService(
            repository, new FakePixPayloadBuilder(), new FakeQrCodeImageGenerator(), new FakeClock(Now));
        await service.CreateWorkerAsync(ValidRequest());
        var worker = repository.Workers.Single();
        worker.Deactivate(Now);

        var result = await service.GetPublicProfileAsync("joao-garcom");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.NotFound, result.Error!.Kind);
    }

    [Fact]
    public async Task GetQrCodePngAsync_WithZeroOrNegativeAmount_ReturnsValidationError()
    {
        var (service, _) = CreateSut();
        await service.CreateWorkerAsync(ValidRequest());

        var result = await service.GetQrCodePngAsync("joao-garcom", 0m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
    }

    [Fact]
    public async Task GetQrCodePngAsync_WithValidAmount_ReturnsPngBytes()
    {
        var (service, _) = CreateSut();
        await service.CreateWorkerAsync(ValidRequest());

        var result = await service.GetQrCodePngAsync("joao-garcom", 10m);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!);
    }
}

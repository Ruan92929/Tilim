using System.Net;
using System.Net.Http.Json;
using Tilim.Api.Contracts;
using Tilim.Application.Workers;
using Tilim.Domain.Enums;

namespace Tilim.Api.Tests;

public class WorkerEndpointsTests : IClassFixture<TilimApiFactory>
{
    private readonly HttpClient _client;

    public WorkerEndpointsTests(TilimApiFactory factory) => _client = factory.CreateClient();

    private static CreateWorkerApiRequest ValidRequest(string slug) => new(
        slug,
        "Maria, manicure",
        "maria@example.com",
        PixKeyType.Email,
        "Belo Horizonte",
        [3m, 6m, 15m],
        "Obrigada pela gorjeta!");

    [Fact]
    public async Task PostWorkers_ThenGetPublicProfile_RoundTrips()
    {
        var slug = $"maria-{Guid.NewGuid():N}"[..20];

        var createResponse = await _client.PostAsJsonAsync("/api/workers", ValidRequest(slug));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var profileResponse = await _client.GetAsync($"/api/w/{slug}");
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);

        var profile = await profileResponse.Content.ReadFromJsonAsync<WorkerPublicProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("Maria, manicure", profile!.DisplayName);
        Assert.Equal([3m, 6m, 15m], profile.SuggestedAmounts);
        Assert.StartsWith("000201", profile.PixCopyPaste);
    }

    [Fact]
    public async Task PostWorkers_WithDuplicateSlug_Returns409()
    {
        var slug = $"dup-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/workers", ValidRequest(slug));

        var secondResponse = await _client.PostAsJsonAsync("/api/workers", ValidRequest(slug));

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetPublicProfile_ForUnknownSlug_Returns404()
    {
        var response = await _client.GetAsync("/api/w/nao-existe-de-verdade");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetQrCode_ForExistingWorker_ReturnsPngImage()
    {
        var slug = $"qr-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/workers", ValidRequest(slug));

        var response = await _client.GetAsync($"/api/w/{slug}/qrcode.png?amount=10.00");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task GetQrCode_WithNegativeAmount_Returns400()
    {
        var slug = $"neg-{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/workers", ValidRequest(slug));

        var response = await _client.GetAsync($"/api/w/{slug}/qrcode.png?amount=-5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

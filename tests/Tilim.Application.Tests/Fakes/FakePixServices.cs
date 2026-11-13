using Tilim.Application.Abstractions;

namespace Tilim.Application.Tests.Fakes;

/// <summary>
/// Stand-in for the real BR Code builder (Tilim.Infrastructure.Pix) so
/// Application-layer tests exercise WorkerService's own logic (slug
/// conflicts, not-found handling, active/inactive filtering) without
/// depending on Infrastructure — that project has its own, much more
/// detailed tests for the real Pix payload format.
/// </summary>
public sealed class FakePixPayloadBuilder : IPixPayloadBuilder
{
    public string Build(PixPayloadRequest request) =>
        $"FAKE-PAYLOAD:{request.PixKey}:{request.Amount?.ToString() ?? "open"}";
}

public sealed class FakeQrCodeImageGenerator : IQrCodeImageGenerator
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 10) =>
        System.Text.Encoding.UTF8.GetBytes($"FAKE-PNG:{payload}");
}

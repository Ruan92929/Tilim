using QRCoder;
using Tilim.Application.Abstractions;

namespace Tilim.Infrastructure.QrCodes;

/// <summary>Thin adapter over QRCoder (github.com/codebude/QRCoder).</summary>
public sealed class QrCoderPngGenerator : IQrCodeImageGenerator
{
    public byte[] GeneratePng(string payload, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var pngQrCode = new PngByteQRCode(data);
        return pngQrCode.GetGraphic(pixelsPerModule);
    }
}

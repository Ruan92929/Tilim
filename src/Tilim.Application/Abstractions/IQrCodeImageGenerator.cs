namespace Tilim.Application.Abstractions;

/// <summary>
/// Renders a payload string (the Pix BR Code) into a scannable PNG image.
/// Kept behind an interface so the QR rendering library (QRCoder) is an
/// infrastructure detail, not something the Application layer depends on.
/// </summary>
public interface IQrCodeImageGenerator
{
    byte[] GeneratePng(string payload, int pixelsPerModule = 10);
}

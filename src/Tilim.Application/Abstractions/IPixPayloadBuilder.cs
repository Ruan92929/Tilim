namespace Tilim.Application.Abstractions;

/// <summary>
/// Request to build a Pix "BR Code" payload (the string that gets encoded
/// into the QR Code / "Pix Copia e Cola"). Amount is optional: when it is
/// null, the payer's own banking app asks them how much to send, which is
/// exactly what a tip jar needs for "outro valor".
/// </summary>
public sealed record PixPayloadRequest(
    string PixKey,
    string MerchantName,
    string MerchantCity,
    decimal? Amount,
    string? TransactionId);

/// <summary>
/// Builds the EMV/BR Code payload for a static Pix QR Code, following the
/// Banco Central "Manual de Padrões para Iniciação do Pix" (DICT/BR Code
/// field layout) — see docs/ARCHITECTURE.md for the field-by-field mapping
/// and the official source.
/// </summary>
public interface IPixPayloadBuilder
{
    string Build(PixPayloadRequest request);
}

using System.Globalization;
using System.Text;
using Tilim.Application.Abstractions;

namespace Tilim.Infrastructure.Pix;

/// <summary>
/// Builds the EMV/BR Code payload for a static Pix QR Code / "Pix Copia e
/// Cola". Field layout per the field map in docs/ARCHITECTURE.md; verified
/// against the Banco Central manual's worked example — see
/// BrCodePayloadBuilderTests.KnownBacenExample_MatchesDocumentedCrc.
/// </summary>
public sealed class BrCodePayloadBuilder : IPixPayloadBuilder
{
    private const int MaxMerchantNameLength = 25;
    private const int MaxMerchantCityLength = 15;
    private const int MaxTxIdLength = 25;

    public string Build(PixPayloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PixKey))
            throw new ArgumentException("A chave Pix é obrigatória.", nameof(request));

        var name = PixTextSanitizer.Sanitize(request.MerchantName, MaxMerchantNameLength);
        var city = PixTextSanitizer.Sanitize(request.MerchantCity, MaxMerchantCityLength);
        var txId = SanitizeTxId(request.TransactionId);

        var builder = new StringBuilder();

        AppendField(builder, "00", "01");
        AppendMerchantAccountInformation(builder, request.PixKey);
        AppendField(builder, "52", "0000");
        AppendField(builder, "53", "986");

        if (request.Amount is { } amount)
            AppendField(builder, "54", amount.ToString("0.00", CultureInfo.InvariantCulture));

        AppendField(builder, "58", "BR");
        AppendField(builder, "59", name);
        AppendField(builder, "60", city);
        AppendAdditionalDataField(builder, txId);

        // CRC covers the payload so far, including this "6304" header.
        builder.Append("6304");
        var crc = Crc16Ccitt.ComputeHex(builder.ToString());
        builder.Append(crc);

        return builder.ToString();
    }

    private static void AppendMerchantAccountInformation(StringBuilder builder, string pixKey)
    {
        var inner = new StringBuilder();
        AppendField(inner, "00", "br.gov.bcb.pix");
        AppendField(inner, "01", pixKey);
        AppendField(builder, "26", inner.ToString());
    }

    private static void AppendAdditionalDataField(StringBuilder builder, string txId)
    {
        var inner = new StringBuilder();
        AppendField(inner, "05", txId);
        AppendField(builder, "62", inner.ToString());
    }

    private static void AppendField(StringBuilder builder, string id, string value)
    {
        builder.Append(id);
        builder.Append(value.Length.ToString("D2", CultureInfo.InvariantCulture));
        builder.Append(value);
    }

    private static string SanitizeTxId(string? transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return "***";

        var filtered = new string(transactionId.Where(char.IsLetterOrDigit).ToArray());
        if (filtered.Length == 0)
            return "***";

        return filtered.Length > MaxTxIdLength ? filtered[..MaxTxIdLength] : filtered;
    }
}

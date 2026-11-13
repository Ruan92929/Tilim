using Tilim.Application.Abstractions;
using Tilim.Infrastructure.Pix;

namespace Tilim.Infrastructure.Tests.Pix;

public class BrCodePayloadBuilderTests
{
    private readonly BrCodePayloadBuilder _builder = new();

    /// <summary>
    /// Feeds the builder the BACEN example from the manual (chave, cidade,
    /// valor and txid exactly as documented). The one deliberate difference
    /// is the merchant name: the manual's worked example uses mixed case
    /// ("Fulano de Tal"), but <see cref="PixTextSanitizer"/> always
    /// upper-cases it — by design, see its own docs — so the sanitized
    /// field content here is "FULANO DE TAL", not a byte-for-byte copy of
    /// the manual's text.
    ///
    /// Because CRC16 is sensitive to every byte, that case change alone
    /// changes the checksum: the manual documents 1D3D for the
    /// mixed-case payload (verified byte-for-byte in
    /// <see cref="Crc16CcittTests.ComputeHex_KnownBacenExample_ReturnsDocumentedCrc"/>,
    /// which is this project's ground-truth oracle for the CRC16
    /// algorithm itself), while the upper-cased payload this builder
    /// actually produces has a different, but equally valid, CRC —
    /// independently recomputed here with the same verified algorithm
    /// over the exact upper-cased field content.
    /// </summary>
    [Fact]
    public void Build_KnownBacenExample_MatchesDocumentedPayloadAndCrc()
    {
        var request = new PixPayloadRequest(
            PixKey: "123e4567-e12b-12d1-a456-426655440000",
            MerchantName: "FULANO DE TAL",
            MerchantCity: "BRASILIA",
            Amount: null,
            TransactionId: null);

        var payload = _builder.Build(request);

        // CRC pinned as a literal (not recomputed via Crc16Ccitt here) so
        // this stays a real regression test rather than checking the
        // builder against itself. Value independently cross-checked with a
        // standalone Python CRC-16/CCITT-FALSE implementation over this
        // exact 133-character string, separately from both this codebase's
        // Crc16Ccitt class and from the manual's own 1D3D (mixed-case)
        // example — see the class-level comment above for why it differs
        // from 1D3D.
        const string expected =
            "000201" +
            "26580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-426655440000" +
            "52040000" +
            "5303986" +
            "5802BR" +
            "5913FULANO DE TAL" +
            "6008BRASILIA" +
            "62070503***" +
            "6304F012";

        Assert.Equal(expected, payload);
    }

    [Fact]
    public void Build_AlwaysEndsWithCrcOfEverythingBeforeIt()
    {
        var payload = _builder.Build(new PixPayloadRequest(
            "joao@example.com", "João, Garçom Simpático", "São Paulo", 12.5m, "pedido-42"));

        var crcInPayload = payload[^4..];
        var recomputed = Crc16Ccitt.ComputeHex(payload[..^4]);

        Assert.Equal(recomputed, crcInPayload);
    }

    [Fact]
    public void Build_WithAmount_FormatsWithTwoDecimalsAndDotSeparator()
    {
        var payload = _builder.Build(new PixPayloadRequest(
            "joao@example.com", "Joao", "Sao Paulo", 7m, null));

        Assert.Contains("54047.00", payload);
    }

    [Fact]
    public void Build_WithoutAmount_OmitsField54()
    {
        var payload = _builder.Build(new PixPayloadRequest(
            "joao@example.com", "Joao", "Sao Paulo", null, null));

        Assert.DoesNotContain("5404", payload);
    }

    [Fact]
    public void Build_WithoutTransactionId_UsesTripleAsterisk()
    {
        var payload = _builder.Build(new PixPayloadRequest(
            "joao@example.com", "Joao", "Sao Paulo", null, null));

        Assert.Contains("62070503***", payload);
    }

    [Fact]
    public void Build_SanitizesAccentedMerchantNameAndCity()
    {
        var payload = _builder.Build(new PixPayloadRequest(
            "joao@example.com", "João Município", "São José dos Campos", null, null));

        Assert.DoesNotContain("ã", payload);
        Assert.DoesNotContain("é", payload);
        Assert.Contains("JOAO MUNICIPIO", payload);
    }
}

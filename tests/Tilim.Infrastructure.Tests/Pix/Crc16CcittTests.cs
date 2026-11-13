using Tilim.Infrastructure.Pix;

namespace Tilim.Infrastructure.Tests.Pix;

public class Crc16CcittTests
{
    /// <summary>
    /// The exact payload-minus-CRC string from the worked example in
    /// section 2.6 of the Banco Central "Manual de Padrões para Iniciação
    /// do Pix" (chave 123e4567-e12b-12d1-a456-426655440000, nome "Fulano de
    /// Tal", cidade "BRASILIA", sem valor, txid "***"). The manual states
    /// this example's CRC16 is 1D3D — this test is the ground-truth oracle
    /// for the whole Pix payload feature.
    /// </summary>
    [Fact]
    public void ComputeHex_KnownBacenExample_ReturnsDocumentedCrc()
    {
        const string payloadBeforeCrc =
            "000201" +
            "26580014br.gov.bcb.pix0136123e4567-e12b-12d1-a456-426655440000" +
            "52040000" +
            "5303986" +
            "5802BR" +
            "5913Fulano de Tal" +
            "6008BRASILIA" +
            "62070503***" +
            "6304";

        Assert.Equal("1D3D", Crc16Ccitt.ComputeHex(payloadBeforeCrc));
    }

    [Fact]
    public void ComputeHex_DifferentInputs_ProduceDifferentCrcs()
    {
        var crcA = Crc16Ccitt.ComputeHex("000201260102");
        var crcB = Crc16Ccitt.ComputeHex("000201260103");

        Assert.NotEqual(crcA, crcB);
    }
}

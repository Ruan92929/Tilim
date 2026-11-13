using System.Text;

namespace Tilim.Infrastructure.Pix;

/// <summary>
/// CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF) — the checksum the EMV/BR
/// Code standard uses for field 63. Verified against the Banco Central
/// manual's worked example; see Crc16CcittTests.
/// </summary>
public static class Crc16Ccitt
{
    private const ushort Polynomial = 0x1021;
    private const ushort InitialValue = 0xFFFF;

    public static string ComputeHex(string payload)
    {
        ushort crc = InitialValue;
        foreach (byte b in Encoding.ASCII.GetBytes(payload))
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0
                    ? (ushort)((crc << 1) ^ Polynomial)
                    : (ushort)(crc << 1);
            }
        }

        return crc.ToString("X4");
    }
}

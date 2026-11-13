using Tilim.Infrastructure.Pix;

namespace Tilim.Infrastructure.Tests.Pix;

public class PixTextSanitizerTests
{
    [Theory]
    [InlineData("João's Café", "JOAOS CAFE")]
    [InlineData("BRASILIA", "BRASILIA")]
    [InlineData("São Paulo", "SAO PAULO")]
    public void Sanitize_StripsAccentsAndPunctuation_AndUppercases(string input, string expected)
    {
        Assert.Equal(expected, PixTextSanitizer.Sanitize(input, maxLength: 25));
    }

    [Fact]
    public void Sanitize_TruncatesToMaxLength()
    {
        var result = PixTextSanitizer.Sanitize("Um Nome Bem Comprido De Verdade Mesmo", maxLength: 10);
        Assert.True(result.Length <= 10);
    }

    [Fact]
    public void Sanitize_EmptyAfterStripping_FallsBackToTilim()
    {
        Assert.Equal("TILIM", PixTextSanitizer.Sanitize("!!!???", maxLength: 25));
    }
}

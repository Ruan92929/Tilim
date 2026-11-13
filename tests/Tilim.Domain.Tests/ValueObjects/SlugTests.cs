using Tilim.Domain.Exceptions;
using Tilim.Domain.ValueObjects;

namespace Tilim.Domain.Tests.ValueObjects;

public class SlugTests
{
    [Theory]
    [InlineData("joao-garcom")]
    [InlineData("maria123")]
    [InlineData("abc")]
    public void Create_AcceptsValidFormats(string raw)
    {
        var slug = Slug.Create(raw);
        Assert.Equal(raw, slug.Value);
    }

    [Fact]
    public void Create_NormalizesToLowerCase()
    {
        var slug = Slug.Create("Joao-Garcom");
        Assert.Equal("joao-garcom", slug.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")] // too short
    [InlineData("-joao")] // starts with hyphen
    [InlineData("joao-")] // ends with hyphen
    [InlineData("joão")] // accented char
    [InlineData("joao_garcom")] // underscore not allowed
    public void Create_RejectsInvalidFormats(string raw)
    {
        Assert.Throws<DomainException>(() => Slug.Create(raw));
    }

    [Fact]
    public void Create_RejectsTooLong()
    {
        var tooLong = new string('a', Slug.MaxLength + 1);
        Assert.Throws<DomainException>(() => Slug.Create(tooLong));
    }

    [Fact]
    public void TwoSlugsWithSameValue_AreEqual()
    {
        Assert.Equal(Slug.Create("joao"), Slug.Create("JOAO"));
    }
}

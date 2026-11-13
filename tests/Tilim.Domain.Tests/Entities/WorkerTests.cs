using Tilim.Domain.Entities;
using Tilim.Domain.Enums;
using Tilim.Domain.Exceptions;

namespace Tilim.Domain.Tests.Entities;

public class WorkerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 12, 0, 0, TimeSpan.Zero);

    private static Worker CreateValidWorker(
        string slug = "joao-garcom",
        string displayName = "João, garçom",
        string pixKey = "joao@example.com",
        PixKeyType pixKeyType = PixKeyType.Email,
        string city = "São Paulo",
        IEnumerable<decimal>? amounts = null,
        string? thankYouMessage = "Muito obrigado pela gorjeta!") =>
        Worker.Create(slug, displayName, pixKey, pixKeyType, city, amounts ?? [2m, 5m, 10m], thankYouMessage, Now);

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var worker = CreateValidWorker();

        Assert.Equal("joao-garcom", worker.Slug.Value);
        Assert.True(worker.IsActive);
        Assert.Equal(Now, worker.CreatedAtUtc);
        Assert.Equal([2m, 5m, 10m], worker.SuggestedAmounts);
    }

    [Fact]
    public void Create_SortsSuggestedAmountsAscending()
    {
        var worker = CreateValidWorker(amounts: [10m, 2m, 5m]);
        Assert.Equal([2m, 5m, 10m], worker.SuggestedAmounts);
    }

    [Fact]
    public void Create_WithEmptyDisplayName_Throws()
    {
        Assert.Throws<DomainException>(() => CreateValidWorker(displayName: " "));
    }

    [Fact]
    public void Create_WithNoSuggestedAmounts_Throws()
    {
        Assert.Throws<DomainException>(() => CreateValidWorker(amounts: []));
    }

    [Fact]
    public void Create_WithTooManySuggestedAmounts_Throws()
    {
        var tooMany = Enumerable.Range(1, Worker.MaxSuggestedAmounts + 1).Select(i => (decimal)i);
        Assert.Throws<DomainException>(() => CreateValidWorker(amounts: tooMany));
    }

    [Fact]
    public void Create_WithNegativeSuggestedAmount_Throws()
    {
        Assert.Throws<DomainException>(() => CreateValidWorker(amounts: [-5m]));
    }

    [Theory]
    [InlineData(PixKeyType.Cpf, "12345678901", true)]
    [InlineData(PixKeyType.Cpf, "123", false)]
    [InlineData(PixKeyType.Email, "nome@dominio.com", true)]
    [InlineData(PixKeyType.Email, "nao-e-email", false)]
    [InlineData(PixKeyType.Telefone, "+5511987654321", true)]
    [InlineData(PixKeyType.Telefone, "11987654321", false)] // falta o "+<país>"
    [InlineData(PixKeyType.Aleatoria, "123e4567-e12b-12d1-a456-426655440000", true)]
    [InlineData(PixKeyType.Aleatoria, "nao-e-um-guid", false)]
    public void Create_ValidatesPixKeyFormatPerType(PixKeyType type, string key, bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            var worker = CreateValidWorker(pixKey: key, pixKeyType: type);
            Assert.Equal(key, worker.PixKey);
        }
        else
        {
            Assert.Throws<DomainException>(() => CreateValidWorker(pixKey: key, pixKeyType: type));
        }
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalse()
    {
        var worker = CreateValidWorker();
        var later = Now.AddDays(1);

        worker.Deactivate(later);

        Assert.False(worker.IsActive);
        Assert.Equal(later, worker.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateProfile_ReplacesSuggestedAmounts()
    {
        var worker = CreateValidWorker(amounts: [2m, 5m]);
        worker.UpdateProfile(
            worker.DisplayName, worker.PixKey, worker.PixKeyType, worker.City,
            [1m, 3m, 7m], worker.ThankYouMessage, Now.AddHours(1));

        Assert.Equal([1m, 3m, 7m], worker.SuggestedAmounts);
    }
}

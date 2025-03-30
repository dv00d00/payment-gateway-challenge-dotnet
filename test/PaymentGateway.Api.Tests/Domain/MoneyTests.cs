using FluentAssertions;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Should_Create_Valid_Money()
    {
        var currency = Currency.TryCreate("USD").Value;
        var result = Money.TryCreate(1000, currency);

        result.IsSuccess.Should().BeTrue();
        result.Value.MinorCurrencyUnitCount.Should().Be(1000);
        result.Value.Currency.Value.Should().Be("USD");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Int32.MinValue)]
    public void Should_Fail_When_Amount_Is_Non_Positive(int amount)
    {
        var currency = Currency.TryCreate("USD").Value;
        var result = Money.TryCreate(amount, currency);

        result.IsSuccess.Should().BeFalse();
    }
}
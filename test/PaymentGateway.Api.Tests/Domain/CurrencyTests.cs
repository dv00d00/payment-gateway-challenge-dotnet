using FluentAssertions;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Tests.Domain;

public class CurrencyTests
{
    [Theory]
    [InlineData("usd")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void Should_Create_Valid_Currency(string input)
    {
        var result = Currency.TryCreate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(input.ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("$")]
    [InlineData("💰")]
    [InlineData("usdollars")]
    [InlineData("ABC")]
    public void Should_Fail_Invalid_Currency(string input)
    {
        var result = Currency.TryCreate(input);

        result.IsSuccess.Should().BeFalse();
    }
}
using FluentAssertions;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Tests.Domain;

public class CardNumberTests
{
    [Fact]
    public void Should_Create_Valid_CardNumber()
    {
        var result = CardNumber.TryCreate("4111111111111111");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("4111111111111111");
        result.Value.LastFour.Value.Should().Be(1111);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345678901234567890")] // too long
    [InlineData("4111-1111-1111-1111")]  // non-numeric
    public void Should_Fail_Invalid_CardNumber(string? input)
    {
        var result = CardNumber.TryCreate(input);

        result.IsSuccess.Should().BeFalse();
    }
}
using FluentAssertions;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Tests.Domain;

public class CVVTests
{
    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Should_Create_Valid_CVV(string input)
    {
        var result = CVV.TryCreate(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(input);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("12A")]
    public void Should_Fail_Invalid_CVV(string input)
    {
        var result = CVV.TryCreate(input);

        result.IsSuccess.Should().BeFalse();
    }
}
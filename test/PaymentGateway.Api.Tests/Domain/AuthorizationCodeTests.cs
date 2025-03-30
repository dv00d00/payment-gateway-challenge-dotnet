using FluentAssertions;

using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Tests.Domain;

public class AuthorizationCodeTests
{
    [Fact]
    public void Should_Create_Valid_Code()
    {
        var result = AuthorizationCode.TryCreate("AUTH123");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("AUTH123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_Code_Is_Null_Or_Empty(string? input)
    {
        var result = AuthorizationCode.TryCreate(input);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == ErrorCodes.AcquiringBank.AuthorizationCodeInvalid);
    }
}
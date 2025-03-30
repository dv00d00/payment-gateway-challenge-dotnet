using FluentAssertions;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Tests.Domain;

public class ExpiryDateTests
{
    [Fact]
    public void Should_Create_Valid_ExpiryDate()
    {
        var result = ExpiryDate.TryCreate(12, 2030);

        result.IsSuccess.Should().BeTrue();
        result.Value.Month.Should().Be(12);
        result.Value.Year.Should().Be(2030);
    }

    [Theory]
    [InlineData(0, 2030)]
    [InlineData(13, 2030)]
    [InlineData(6, 1800)]
    [InlineData(6, 10000)]
    public void Should_Fail_Invalid_ExpiryDate(int month, int year)
    {
        var result = ExpiryDate.TryCreate(month, year);

        result.IsSuccess.Should().BeFalse();
    }
}
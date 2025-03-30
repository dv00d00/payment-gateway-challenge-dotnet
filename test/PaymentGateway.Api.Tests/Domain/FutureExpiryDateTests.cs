using FluentAssertions;

using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Domain;

public class FutureExpiryDateTests
{
    private class FakeSystemTime : ISystemTime
    {
        public DateOnly UtcToday { get; set; } = new DateOnly(2025, 03, 30);
    }

    [Fact]
    public void Should_Create_Valid_FutureExpiryDate()
    {
        var expiry = ExpiryDate.TryCreate(12, 2025).Value;
        var systemTime = new FakeSystemTime { UtcToday = new DateOnly(2025, 03, 30) };

        var result = FutureExpiryDate.TryCreate(expiry, systemTime);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_If_Expiry_Year_Is_Past()
    {
        var expiry = ExpiryDate.TryCreate(12, 2024).Value;
        var systemTime = new FakeSystemTime { UtcToday = new DateOnly(2025, 03, 30) };

        var result = FutureExpiryDate.TryCreate(expiry, systemTime);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Should_Fail_If_Expiry_Month_Is_Past_In_Same_Year()
    {
        var expiry = ExpiryDate.TryCreate(2, 2025).Value;
        var systemTime = new FakeSystemTime { UtcToday = new DateOnly(2025, 03, 30) };

        var result = FutureExpiryDate.TryCreate(expiry, systemTime);

        result.IsSuccess.Should().BeFalse();
    }
}
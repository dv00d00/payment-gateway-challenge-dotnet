using FsCheck.Xunit;
using FsCheck.Fluent;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Services;

using Xunit.Abstractions;

namespace PaymentGateway.Api.Tests;

public class PaymentRequestValidator_RandomizedTests(ITestOutputHelper output)
{
    private class FakeTime : ISystemTime
    {
        public DateOnly UtcToday { get; init; } 
    }

    [Theory]
    [InlineData(PaymentStatus.Authorized)]
    [InlineData(PaymentStatus.Declined)]
    public void ValidGeneratedRequest_IsValidViaPaymentValidator(PaymentStatus outcome)
    {
        var now = DateTime.UtcNow;
        var time = new FakeTime { UtcToday = DateOnly.FromDateTime(now) };
        var sut = new PaymentValidator(time);
        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(outcome, now).ToArbitrary();
        
        Prop.ForAll(arb, request => sut.Validate(request).IsSuccess == true)
            .QuickCheckThrowOnFailure(output);
    }
    
    [Fact]
    public void InvalidGeneratedRequest_IsInvalidViaPaymentValidator()
    {
        var now = DateTime.UtcNow;
        var time = new FakeTime { UtcToday = DateOnly.FromDateTime(now) };
        var sut = new PaymentValidator(time);
        var arb = PaymentRequestGenerators.InvalidPaymentRequestGen().ToArbitrary();
        
        Prop.ForAll(arb, request => sut.Validate(request).IsSuccess == false)
            .QuickCheckThrowOnFailure(output);
    }
}
using FsCheck.Fluent;
using FsCheck.Xunit;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Services;

using Xunit.Abstractions;

namespace PaymentGateway.Api.Tests;

public class HashTests(ITestOutputHelper output)
{
    [Fact]
    public void PaymentRequestsEquality_ShouldMatchHashesEquality()
    {
        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(PaymentStatus.Authorized, DateTime.UtcNow);
        var validator = new PaymentValidator(new FixedSystemTime(DateOnly.FromDateTime(DateTime.Now)));
        
        // source equality implies hash equality
        Prop.ForAll(arb.Two().ToArbitrary(), gen =>
        {
            var (r1, r2) = gen;
            PaymentDetails paymentDetails1 = validator.Validate(r1).Value;
            PaymentDetails paymentDetails2 = validator.Validate(r2).Value;

            var hash1 = RequestHasher.ComputeHash(paymentDetails1);
            var hash2 = RequestHasher.ComputeHash(paymentDetails2);
            
            Assert.Equal(paymentDetails1 == paymentDetails2, hash1 == hash2);

        }).QuickCheckThrowOnFailure(output);
    }
    
    [Fact]
    public void EqualPaymentRequests_ProduceEqualHashes()
    {
        var arb = PaymentRequestGenerators.ValidPaymentRequestGen(PaymentStatus.Authorized, DateTime.UtcNow);
        var validator = new PaymentValidator(new FixedSystemTime(DateOnly.FromDateTime(DateTime.Now)));
        
        Prop.ForAll(arb.ToArbitrary(), r1 =>
        {
            var hash1 = RequestHasher.ComputeHash(validator.Validate(r1).Value);
            var hash2 = RequestHasher.ComputeHash(validator.Validate(r1).Value);
            
            Assert.Equal(hash1, hash2);

        }).QuickCheckThrowOnFailure(output);
    }
}
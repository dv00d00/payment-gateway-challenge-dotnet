using FsCheck.Xunit;
using FsCheck.Fluent;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

using Xunit.Abstractions;

namespace PaymentGateway.Api.Tests;

public class PaymentRequestValidator_ExampleBasedTests(ITestOutputHelper output)
{
    private class FakeTime : ISystemTime
    {
        public DateOnly UtcToday { get; init; } 
    }
    
    private static readonly DateTime Now = DateTime.UtcNow;
    
    public static IEnumerable<object[]> ValidExamples =>
        new List<object[]>
        {
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "4111111111111111",     // 16-digit numeric
                    ExpiryMonth = 12,
                    ExpiryYear = Now.Year + 1,
                    Currency = "USD",
                    Amount = 5000,
                    CVV = "123"
                }
            },
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "5555555555554444",     // 16-digit numeric
                    ExpiryMonth = 1,
                    ExpiryYear = Now.Year + 1,
                    Currency = "EUR",
                    Amount = 100,
                    CVV = "1234"
                }
            },
            new object[]
            {
                new PostPaymentRequest
                {
                    CardNumber = "12345678901234",       // 14-digit numeric (min length)
                    ExpiryMonth = DateTime.UtcNow.Month + 1,
                    ExpiryYear = Now.Year,
                    Currency = "GBP",
                    Amount = 1,
                    CVV = "999"
                }
            }
        };
    
    public static IEnumerable<object[]> InvalidExamples =>
    new List<object[]>
    {
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "",                     // empty
                ExpiryMonth = 5,
                ExpiryYear = Now.Year,
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "abcd1234efgh5678",     // non-numeric
                ExpiryMonth = 5,
                ExpiryYear = Now.Year,
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "123456",               // too short
                ExpiryMonth = 5,
                ExpiryYear = Now.Year,
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 0,                     // invalid month
                ExpiryYear = Now.Year,
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 12,
                ExpiryYear = Now.Year - 1, // past year
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 12,
                ExpiryYear = 10000,                 // year too large
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 1,
                ExpiryYear = Now.Year,  // already expired this year/month
                Currency = "USD",
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 5,
                ExpiryYear = Now.Year,
                Currency = "CAD",                   // not allowed
                Amount = 100,
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 5,
                ExpiryYear = Now.Year,
                Currency = "USD",
                Amount = 0,                         // amount must be > 0
                CVV = "123"
            }
        },
        new object[]
        {
            new PostPaymentRequest
            {
                CardNumber = "4111111111111111",
                ExpiryMonth = 5,
                ExpiryYear = Now.Year,
                Currency = "USD",
                Amount = 100,
                CVV = "12a"                         // non-numeric CVV
            }
        }
    };
    
    [Theory]
    [MemberData(nameof(ValidExamples))]
    public void Validator_Should_Pass_For_Valid_Requests(PostPaymentRequest request)
    {
        var time = new FakeTime { UtcToday = DateOnly.FromDateTime(Now) };
        var sut = new PaymentValidator(time);
        Assert.True(sut.Validate(request).IsSuccess);
    }

    [Theory]
    [MemberData(nameof(InvalidExamples))]
    public void Validator_Should_Fail_For_Invalid_Requests(PostPaymentRequest request)
    {
        var time = new FakeTime { UtcToday = DateOnly.FromDateTime(Now) };
        var sut = new PaymentValidator(time);
        Assert.False(sut.Validate(request).IsSuccess);
    }
}
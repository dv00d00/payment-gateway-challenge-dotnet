using FsCheck;
using FsCheck.Fluent;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public static class PaymentRequestGenerators
{
    // Allowed ISO currency codes.
    private static readonly string[] AllowedCurrencies = ["USD", "EUR", "GBP"];

    // Helper: Given a desired card outcome, return the allowed last digit(s).
    private static char[] GetLastDigits(PaymentStatus outcome) =>
        outcome switch
        {
            PaymentStatus.Rejected => ['0'],
            PaymentStatus.Authorized => ['1', '3', '5', '7', '9'],
            PaymentStatus.Declined => ['2', '4', '6', '8'],
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };

    // Generates a numeric string with a fixed length.
    private static Gen<string> NumericStringGen(int length) =>
        Gen.Elements("0123456789".ToCharArray()).ArrayOf(length)
            .Select(arr => new string(arr));

    // Generates a credit card number with a total length between 14 and 19 digits,
    // ensuring the last digit corresponds to the desired outcome.
    private static Gen<string> CardNumberGen(PaymentStatus outcome) =>
        from len in Gen.Choose(14, 19)
        from prefix in NumericStringGen(len - 1)
        from lastDigit in Gen.Elements(GetLastDigits(outcome))
        select prefix + lastDigit;

    // Generate a valid expiry date.
    // The expiry year is generated from the current year up to current year + 10.
    // If the expiry year is the current year, the month is chosen between the current month and 12.
    private static Gen<(int Month, int Year)> ExpiryGen(DateTime now) =>
        from year in Gen.Choose(now.Year + 1, now.Year + 10)
        from month in year == now.Year
            ? Gen.Choose(now.Month, 12)
            : Gen.Choose(1, 12)
        select (month, year);

    // Generator for currency from the allowed list.
    private static Gen<string> CurrencyGen() =>
        Gen.Elements(AllowedCurrencies);

    // Generator for a positive amount in minimal currency units (int > 0).
    // For example, this could represent cents.
    private static Gen<int> AmountGen() =>
        Gen.Choose(100, 1000000); // Adjust the upper bound as needed

    // Generator for CVV: a numeric string of length 3 or 4.
    private static Gen<string> CvvGen() =>
        from len in Gen.Elements(3, 4)
        from cvv in NumericStringGen(len)
        select cvv;

    // The full generator for PaymentRequest.
    public static Gen<PostPaymentRequest> ValidPaymentRequestGen(PaymentStatus outcome, DateTime now) =>
        from cardNumber in CardNumberGen(outcome)
        from expiry in ExpiryGen(now)
        from currency in CurrencyGen()
        from amount in AmountGen()
        from cvv in CvvGen()
        select new PostPaymentRequest
        {
            Amount = amount,
            CardNumber = cardNumber,
            Currency = currency,
            CVV = cvv,
            ExpiryMonth = expiry.Month,
            ExpiryYear = expiry.Year
        };
    
    public static Gen<PostPaymentRequest> InvalidPaymentRequestGen() => 
        from cardNumber in ArbMap.Default.GeneratorFor<string>()
        from expiryMonth in ArbMap.Default.GeneratorFor<int>()
        from expiryYear in ArbMap.Default.GeneratorFor<int>()
        from currency in ArbMap.Default.GeneratorFor<string>()
        from amount in ArbMap.Default.GeneratorFor<int>()
        from cvv in ArbMap.Default.GeneratorFor<string>()
        let result = new PostPaymentRequest
        {
            Amount = amount,
            CardNumber = cardNumber,
            Currency = currency,
            CVV = cvv,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear
        }
        select result;
    
    public static Gen<PostPaymentRequest> InvalidPaymentRequestGen(IPaymentValidator validator) => 
        from cardNumber in ArbMap.Default.GeneratorFor<string>()
        from expiryMonth in ArbMap.Default.GeneratorFor<int>()
        from expiryYear in ArbMap.Default.GeneratorFor<int>()
        from currency in ArbMap.Default.GeneratorFor<string>()
        from amount in ArbMap.Default.GeneratorFor<int>()
        from cvv in ArbMap.Default.GeneratorFor<string>()
        let result = new PostPaymentRequest
        {
            Amount = amount,
            CardNumber = cardNumber,
            Currency = currency,
            CVV = cvv,
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear
        }
        where validator.Validate(result).IsSuccess == false
        select result;
}
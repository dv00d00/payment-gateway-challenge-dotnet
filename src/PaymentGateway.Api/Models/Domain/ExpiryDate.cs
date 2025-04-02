using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Types;

using static PaymentGateway.Api.Constants.ErrorCodes.PaymentGateway;

namespace PaymentGateway.Api.Models.Domain;

public record ExpiryDate
{
    public int Month { get; }
    public int Year { get; }
    
    private ExpiryDate(int month, int year)
    {
        Month = month;
        Year = year;
    }
    
    public static Result<ExpiryDate> TryCreate(int? maybeMonth, int? maybeYear) =>
        Result.Combine(ParseMonth(maybeMonth), ParseYear(maybeYear))
            .Select(pair =>
            {
                var (month, year) = pair;
                return new ExpiryDate(month, year);
            });

    private static Result<int> ParseMonth(int? month)
    {
        if (!month.HasValue)
            return Result<int>.Failure(ExpiryMonthRequired, "Expiry month is required.");
        
        if (month < 1 || month > 12)
            return Result<int>.Failure(ExpiryMonthRange, "Expiry month must be between 1 and 12.");
        
        return Result.Success(month.Value);
    }
    
    private static Result<int> ParseYear(int? year)
    {
        if (!year.HasValue)
            return Result<int>.Failure(ExpiryYearRequired, "Expiry year is required.");
        
        if (year < 1970)
            return Result<int>.Failure(ExpiryYearTooSmall, "Expiry year must be greater than 1970.");

        if (year > 9999)
            return Result<int>.Failure(ExpiryYearTooLarge, "Expiry year must be less than 10000.");
        
        return Result.Success(year.Value);
    }
    
    public static ExpiryDate FromFutureExpiryDate(FutureExpiryDate futureExpiryDate) => new(futureExpiryDate.Month, futureExpiryDate.Year);
}
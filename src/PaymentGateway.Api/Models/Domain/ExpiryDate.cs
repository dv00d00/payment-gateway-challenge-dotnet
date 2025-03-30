using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Types;

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
    
    public static Result<ExpiryDate> TryCreate(int month, int year)
    {
        if (month < 1 || month > 12)
            return Result<ExpiryDate>.Failure(new Error(ErrorCodes.PaymentGateway.ExpiryMonthRange, "Expiry month must be between 1 and 12."));

        if (year < 1970)
            return Result<ExpiryDate>.Failure(new Error(ErrorCodes.PaymentGateway.ExpiryYearTooSmall, "Expiry year must be greater than 1970."));

        if (year > 9999)
            return Result<ExpiryDate>.Failure(new Error(ErrorCodes.PaymentGateway.ExpiryYearTooLarge, "Expiry year must be less than 10000."));
        
        return Result<ExpiryDate>.Success(new ExpiryDate(month, year));
    }
    
    public static ExpiryDate FromFutureExpiryDate(FutureExpiryDate futureExpiryDate) => new(futureExpiryDate.Month, futureExpiryDate.Year);
}
using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Models.Domain;

public record FutureExpiryDate
{
    public int Month { get; }
    public int Year { get; }
    public DateOnly AnchorDate { get; }
    
    private FutureExpiryDate(int month, int year, DateOnly anchorDate)
    {
        Month = month;
        Year = year;
        AnchorDate = anchorDate;
    }
    
    public static Result<FutureExpiryDate> TryCreate(ExpiryDate expiryDate, ISystemTime systemTime)
    {
        var today = systemTime.UtcToday;
        
        // The card remains active until the last day of the month listed 
        
        if (expiryDate.Year < today.Year)
            return Result<FutureExpiryDate>.Failure(new Error(ErrorCodes.PaymentGateway.ExpiryYearPast, "The expiry year is in the past."));
        
        if (expiryDate.Year == today.Year && expiryDate.Month < today.Month)
            return Result<FutureExpiryDate>.Failure(new Error(ErrorCodes.PaymentGateway.ExpiryDateInPast, "The expiry date must be in the future."));

        return Result<FutureExpiryDate>.Success(new FutureExpiryDate(expiryDate.Month, expiryDate.Year, today));
    }
}
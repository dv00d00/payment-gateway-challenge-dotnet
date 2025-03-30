using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Models.Domain;

public record Money
{
    public int MinorCurrencyUnitCount { get; }
    public Currency Currency { get; }
    
    private Money(int minorCurrencyUnitCount, Currency currency)
    {
        MinorCurrencyUnitCount = minorCurrencyUnitCount;
        Currency = currency;
    }
    
    public static Result<Money> TryCreate(int amount, Currency currency)
    {
        if (amount <= 0)
            return Result<Money>.Failure(new Error(ErrorCodes.PaymentGateway.AmountNonPositive, "Amount must be greater than zero."));

        return Result<Money>.Success(new Money(amount, currency));
    }
}
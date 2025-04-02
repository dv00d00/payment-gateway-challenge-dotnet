using PaymentGateway.Api.Types;

using static PaymentGateway.Api.Constants.ErrorCodes.PaymentGateway;

namespace PaymentGateway.Api.Models.Domain;

public record CardNumber
{
    private CardNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CardNumber> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CardNumber>.Failure(
                new Error(CardNumberRequired, "Card number is required."));

        if (value.Length is < 14 or > 19)
            return Result<CardNumber>.Failure(
                new Error(CardNumberLength, "Card number must be between 14 and 19 characters."));

        if (!value.All(char.IsDigit))
            return Result<CardNumber>.Failure(
                new Error(CardNumberNumeric, "Card number must only contain numeric characters."));

        return Result.Success(new CardNumber(value));
    }
    
    public CardNumberLastFour LastFour => CardNumberLastFour.FromCardNumber(this);
}
using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Models.Domain;

public record CVV
{
    private CVV(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CVV> TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CVV>.Failure(new Error(ErrorCodes.PaymentGateway.CvvRequired, "CVV is required."));

        if (value.Length is < 3 or > 4)
            return Result<CVV>.Failure(new Error(ErrorCodes.PaymentGateway.CvvLength, "CVV must be between 3 and 4 characters."));

        if (!value.All(char.IsDigit))
            return Result<CVV>.Failure(new Error(ErrorCodes.PaymentGateway.CvvNumeric, "CVV must only contain numeric characters."));

        return Result<CVV>.Success(new CVV(value));
    }
}
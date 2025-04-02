using PaymentGateway.Api.Types;

using static PaymentGateway.Api.Constants.ErrorCodes.PaymentGateway;

namespace PaymentGateway.Api.Models.Domain;

public record CVV
{
    private CVV(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CVV> TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<CVV>.Failure(new Error(CvvRequired, "CVV is required."));

        if (value.Length is < 3 or > 4)
            return Result<CVV>.Failure(new Error(CvvLength, "CVV must be between 3 and 4 characters."));

        if (!value.All(char.IsDigit))
            return Result<CVV>.Failure(new Error(CvvNumeric, "CVV must only contain numeric characters."));

        return Result.Success(new CVV(value));
    }
}
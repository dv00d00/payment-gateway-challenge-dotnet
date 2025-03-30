using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Models.Domain;

public record AuthorizationCode
{
    private AuthorizationCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<AuthorizationCode> TryCreate(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? Result<AuthorizationCode>.Failure(new Error(ErrorCodes.AcquiringBank.AuthorizationCodeInvalid, "Authorization code provided is empty."))
            : Result<AuthorizationCode>.Success(new AuthorizationCode(code));
    }
}
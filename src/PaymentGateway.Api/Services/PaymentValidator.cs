using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Services;

public interface IPaymentValidator
{
    Result<PaymentDetails> Validate(PostPaymentRequest request);
}

public class PaymentValidator(ISystemTime systemTime) : IPaymentValidator
{
    public Result<PaymentDetails> Validate(PostPaymentRequest request)
    {
        var maybeCardNumber = CardNumber.TryCreate(request.CardNumber);

        var maybeFutureExpiryDate = ExpiryDate.TryCreate(request.ExpiryMonth, request.ExpiryYear)
            .Bind(ed => FutureExpiryDate.TryCreate(ed, systemTime));
        
        var maybeMoney = Currency.TryCreate(request.Currency)
            .Bind(currency => Money.TryCreate(request.Amount, currency));
        
        var maybeCvv = CVV.TryCreate(request.CVV);
        
        return Result.Combine(maybeCardNumber, maybeFutureExpiryDate, maybeMoney, maybeCvv)
            .Select(args =>
            {
                var (cn, fed, m, cvv) = args;
                return new PaymentDetails(cn, fed, m, cvv);
            });
    }
}
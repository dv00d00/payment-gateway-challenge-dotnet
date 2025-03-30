using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController() : Controller
{
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), 200)]
    [ProducesResponseType(typeof(ClientErrorResponse), 400)]
    public async Task<ActionResult<PostPaymentResponse>> InitiatePayment(
        [FromServices]IPaymentValidator validator, 
        [FromServices]IBankClient bankClient, 
        [FromServices]PaymentsRepository paymentsRepository,
        
        [FromBody]PostPaymentRequest paymentRequestDto)
    {
        var paymentIntent = validator.Validate(paymentRequestDto);
        if (paymentIntent.IsFailure)
        {
            return Error(paymentIntent.Errors);
        }
        
        var paymentDetails = paymentIntent.Value;
        
        var bankResponse = await bankClient.InitiatePayment(paymentDetails);

        switch (bankResponse)
        {
            case BankResponse.Authorized authorized:
                {
                    var stored = await paymentsRepository.AddAuthorized(paymentDetails, authorized.AuthorizationCode);
                    return Success(stored);
                }
            case BankResponse.Declined:
                {
                    var stored = await paymentsRepository.AddDeclined(paymentDetails);
                    return Success(stored);
                }
            case BankResponse.Rejected rejected:
                return Error(rejected);
            case BankResponse.CommunicationError unknown:
                switch (unknown.Reason)
                {
                    case Justification.UnrecognizedResponse:
                    case Justification.TransportError:
                        return new StatusCodeResult(StatusCodes.Status502BadGateway);
                    case Justification.Timeout:
                        return new StatusCodeResult(StatusCodes.Status504GatewayTimeout);
                    case Justification.Exception:
                    default:
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                }
            default:
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPaymentResponse>> GetPaymentAsync(
        [FromServices]PaymentsRepository paymentsRepository,
        [FromRoute]Guid id)
    {
        var payment = await paymentsRepository.Get(id);

        if (payment is null)
            return NotFound();
        
        return Success(payment);
    }

    private static PostPaymentResponse Success(StoredPayment storedPayment)
    {
        return new PostPaymentResponse 
        { 
            Id = storedPayment.Id, 
            Status = storedPayment.Status, 
            CardNumberLastFour = storedPayment.CardNumberLastFour.Value, 
            ExpiryMonth = storedPayment.ExpiryDate.Month, 
            ExpiryYear = storedPayment.ExpiryDate.Year, 
            Currency = storedPayment.Money.Currency.Value, 
            Amount = storedPayment.Money.MinorCurrencyUnitCount 
        };
    }

    private static BadRequestObjectResult Error(IEnumerable<Error> errors)
    {
        var dto = new ClientErrorResponse 
        {
            Issues = errors.Select(it => new ClientErrorResponse.Issue
            {
                ErrorCode = it.Code,
                ErrorMessage = it.Description,
            }).ToList()
        };
        return new BadRequestObjectResult(dto);
    }
    
    private static BadRequestObjectResult Error(BankResponse.Rejected rejected)
    {
        var dto = new ClientErrorResponse 
        {
            Issues = [
                new ClientErrorResponse.Issue
                {
                    ErrorCode = ErrorCodes.AcquiringBank.RejectedByBank,
                    // todo: unsafe, could leak sensitive data, lack of docs
                    ErrorMessage = rejected.ErrorMessage ?? "Bank rejected the payment",
                }
            ] 
        };
        return new BadRequestObjectResult(dto);
    }
}
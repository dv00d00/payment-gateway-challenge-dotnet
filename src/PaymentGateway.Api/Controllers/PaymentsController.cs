using System.Reflection.Metadata;

using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Constants;
using PaymentGateway.Api.Models.Domain;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Types;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(PaymentsRepository paymentsRepository) : Controller
{
    [HttpPost]
    [ProducesResponseType(typeof(PostPaymentResponse), 200)]
    [ProducesResponseType(typeof(ClientErrorResponse), 400)]
    public async Task<ActionResult<PostPaymentResponse>> InitiatePayment(
        [FromServices] IPaymentValidator validator,
        [FromServices] IBankClient bankClient,
        [FromServices] IdempotencyStore guard,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] PostPaymentRequest paymentRequestDto)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || !Guid.TryParse(idempotencyKey, out var id))
            return Error([
                new(ErrorCodes.Idempotency.MissingIdempotencyKey, "Idempotency-Key header is required and must be a valid GUID")
            ]);

        var paymentIntent = validator.Validate(paymentRequestDto);
        if (paymentIntent.IsFailure)
        {
            return Error(paymentIntent.Errors);
        }

        var paymentDetails = paymentIntent.Value;

        var observed = await guard.TryFindResultOrGetLock(id, paymentDetails);

        switch (observed)
        {
            case IdResult.Cached(var response):
                return await ProcessBankResponse(id, response, paymentDetails);

            case IdResult.NoResultLockObtained(var handle):
                
                var bankResponse = await bankClient.InitiatePayment(paymentDetails);
                await guard.SaveResult(id, paymentDetails, bankResponse);
                await handle.DisposeAsync();

                return await ProcessBankResponse(id, bankResponse, paymentDetails);

            case IdResult.Error(IdEnterError.HashMismatch):
                return Error([
                    new(ErrorCodes.Idempotency.IdempotencyKeyAlreadyUsed, "Idempotency key was already used with different data")
                ]);

            case IdResult.Error(IdEnterError.CantGetLock):
                return StatusCode(StatusCodes.Status429TooManyRequests);

            default:
                return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostPaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostPaymentResponse>> GetPaymentAsync([FromRoute] Guid id)
    {
        var payment = await paymentsRepository.Get(id);
        return payment is null ? NotFound() : Success(payment);
    }

    private async Task<ActionResult<PostPaymentResponse>> ProcessBankResponse(Guid id, BankResponse bankResponse, PaymentDetails details)
    {
        return bankResponse switch
        {
            BankResponse.Authorized authorized =>
                Success(await paymentsRepository.AddAuthorized(id, details, authorized.AuthorizationCode)),

            BankResponse.Declined =>
                Success(await paymentsRepository.AddDeclined(id, details)),

            BankResponse.Rejected rejected =>
                Error(rejected),

            BankResponse.CommunicationError { Reason: Justification.UnrecognizedResponse or Justification.TransportError } =>
                new StatusCodeResult(StatusCodes.Status502BadGateway),

            BankResponse.CommunicationError { Reason: Justification.Timeout } =>
                new StatusCodeResult(StatusCodes.Status504GatewayTimeout),

            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    private static PostPaymentResponse Success(StoredPayment stored) => new()
    {
        Id = stored.Id,
        Status = stored.Status,
        CardNumberLastFour = stored.CardNumberLastFour.Value,
        ExpiryMonth = stored.ExpiryDate.Month,
        ExpiryYear = stored.ExpiryDate.Year,
        Currency = stored.Money.Currency.Value,
        Amount = stored.Money.MinorCurrencyUnitCount
    };

    private static BadRequestObjectResult Error(IEnumerable<Error> errors) => new(
        new ClientErrorResponse
        {
            Issues = errors.Select(e => new ClientErrorResponse.Issue
            {
                ErrorCode = e.Code,
                ErrorMessage = e.Description
            }).ToList()
        });

    private static BadRequestObjectResult Error(BankResponse.Rejected rejected) => new(
        new ClientErrorResponse
        {
            Issues = [
                new ClientErrorResponse.Issue
                {
                    ErrorCode = ErrorCodes.AcquiringBank.RejectedByBank,
                    ErrorMessage = rejected.ErrorMessage ?? "Bank rejected the payment"
                }
            ]
        });
}
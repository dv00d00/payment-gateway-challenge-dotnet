using System.Net;

using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

public enum Justification { UnrecognizedResponse, TransportError, Timeout, Exception }

public abstract record BankResponse
{
    public sealed record Authorized(AuthorizationCode AuthorizationCode) : BankResponse;
    public sealed record Declined : BankResponse;
    public sealed record Rejected(string? ErrorMessage) : BankResponse;
    public sealed record CommunicationError(Justification Reason) : BankResponse;
}

public interface IBankClient
{
    Task<BankResponse> InitiatePayment(PaymentDetails paymentDetails);
}

public class BankClient(IHttpClientFactory httpClientFactory) : IBankClient
{
    private class ok_response
    {
        public bool? authorized { get; init; }
        public string? authorization_code { get; init; }
    }

    private class client_error
    {
        public string? error_message { get; init; }
    }

    private class initiate_payment_request
    {
        public required string card_number { get; init; }
        public required string expiry_date { get; init; }
        public required string currency { get; init; }
        public required int amount { get; init; }
        public required string cvv { get; init; }
    }

    public async Task<BankResponse> InitiatePayment(PaymentDetails paymentDetails)
    {
        var dto = new initiate_payment_request
        {
            card_number = paymentDetails.CardNumber.Value,
            expiry_date = $"{paymentDetails.ExpiryDate.Month}/{paymentDetails.ExpiryDate.Year}",
            currency = paymentDetails.Money.Currency.Value, 
            amount = paymentDetails.Money.MinorCurrencyUnitCount,
            cvv = paymentDetails.CVV.Value
        };

        var client = httpClientFactory.CreateClient("payments");

        try
        {
            var response = await client.PostAsJsonAsync("/payments", dto);

            // todo: differentiate transient/permanent errors
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    {
                        ok_response? received;
                        try
                        {
                            received = await response.Content.ReadFromJsonAsync<ok_response>();
                        }
                        catch (Exception)
                        {
                            return new BankResponse.CommunicationError(Justification.UnrecognizedResponse);
                        }

                        switch (received)
                        {
                            case { authorized: false }:
                                return new BankResponse.Declined();
                            
                            case { authorized: true }:
                                var maybeCode = AuthorizationCode.TryCreate(received.authorization_code);
                                if (maybeCode.IsFailure)
                                    return new BankResponse.CommunicationError(Justification.UnrecognizedResponse);
                                var code = maybeCode.Value;
                                return new BankResponse.Authorized(code);
                            
                            default:
                                return new BankResponse.CommunicationError(Justification.UnrecognizedResponse);
                        }
                    }
                case HttpStatusCode.BadRequest:
                    try
                    {
                        var received = await response.Content.ReadFromJsonAsync<client_error>();
                        return new BankResponse.Rejected(received?.error_message);
                    }
                    catch (Exception)
                    {
                        return new BankResponse.Rejected(null);
                    }
                case HttpStatusCode.RequestTimeout:
                    return new BankResponse.CommunicationError(Justification.TransportError);
                default:
                    return new BankResponse.CommunicationError(Justification.UnrecognizedResponse);
            }
        }
        catch (HttpRequestException)
        {
            return new BankResponse.CommunicationError(Justification.TransportError);
        }
        catch (TaskCanceledException)
        {
            return new BankResponse.CommunicationError(Justification.Timeout);
        }
        catch (Exception)
        {
            return new BankResponse.CommunicationError(Justification.Exception);
        }
    }
}
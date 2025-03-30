namespace PaymentGateway.Api.Models.Domain;

public record PaymentDetails(CardNumber CardNumber, FutureExpiryDate ExpiryDate, Money Money, CVV CVV);
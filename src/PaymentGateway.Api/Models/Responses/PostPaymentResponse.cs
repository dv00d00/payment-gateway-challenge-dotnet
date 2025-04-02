using System.Text.Json.Serialization;

using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Models.Responses;

public class PostPaymentResponse
{
    public required Guid Id { get; init; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required PaymentStatus Status { get; init; }
    
    public required int CardNumberLastFour { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public required int Amount { get; init; }
}
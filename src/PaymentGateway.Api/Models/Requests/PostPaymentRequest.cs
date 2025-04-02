namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    public string? CardNumber { get; set; }
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string? Currency { get; set; }
    public int Amount { get; set; }
    public string? CVV { get; set; }

    public override string ToString() => 
        $"{nameof(CardNumber)}: {CardNumber}, {nameof(ExpiryMonth)}: {ExpiryMonth}, {nameof(ExpiryYear)}: {ExpiryYear}, {nameof(Currency)}: {Currency}, {nameof(Amount)}: {Amount}, {nameof(CVV)}: {CVV}";
}
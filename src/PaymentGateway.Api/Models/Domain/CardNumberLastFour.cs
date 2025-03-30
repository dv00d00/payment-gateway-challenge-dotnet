namespace PaymentGateway.Api.Models.Domain;

public record CardNumberLastFour
{
    private CardNumberLastFour(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static CardNumberLastFour FromCardNumber(CardNumber cardNumber)
    {
        var lastFour = cardNumber.Value[^4..];
        return new CardNumberLastFour(int.Parse(lastFour));
    }
}
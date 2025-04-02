using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

public abstract class StoredPayment
{
    public required Guid Id { get; init; }
    public abstract PaymentStatus Status { get; }
    public required CardNumberLastFour CardNumberLastFour { get; init; }
    public required ExpiryDate ExpiryDate { get; init; }
    public required Money Money { get; init; }
    
    public sealed class Authorized : StoredPayment
    {
        public override PaymentStatus Status => PaymentStatus.Authorized;

        public required string AuthorizationCode { get; init; }
    }

    public sealed class Declined : StoredPayment
    {
        public override PaymentStatus Status => PaymentStatus.Declined;
    }
}

public interface IPaymentsRepository
{
    Task<StoredPayment> AddAuthorized(Guid id, PaymentDetails paymentDetails, AuthorizationCode code);
    Task<StoredPayment> AddDeclined(Guid id, PaymentDetails paymentDetails);
    Task<StoredPayment?> TryFind(Guid id);
}

public class PaymentsRepository : IPaymentsRepository
{
    public readonly Dictionary<Guid, StoredPayment> Payments = new();
    
    public Task<StoredPayment> AddAuthorized(Guid id, PaymentDetails paymentDetails, AuthorizationCode code)
    {
        var payment = new StoredPayment.Authorized 
        { 
            Id = id, 
            CardNumberLastFour = paymentDetails.CardNumber.LastFour, 
            ExpiryDate = ExpiryDate.FromFutureExpiryDate(paymentDetails.ExpiryDate), 
            Money = paymentDetails.Money, 
            AuthorizationCode = code.Value 
        };
        
        Payments[id] = payment;
        
        return Task.FromResult<StoredPayment>(payment);
    }
    
    public Task<StoredPayment> AddDeclined(Guid id, PaymentDetails paymentDetails)
    {
        var payment = new StoredPayment.Declined 
        { 
            Id = id, 
            CardNumberLastFour = paymentDetails.CardNumber.LastFour, 
            ExpiryDate = ExpiryDate.FromFutureExpiryDate(paymentDetails.ExpiryDate), 
            Money = paymentDetails.Money, 
        };
        
        Payments[id] = payment;
        
        return Task.FromResult<StoredPayment>(payment);
    }
    
    public Task<StoredPayment?> TryFind(Guid id)
    {
        return Task.FromResult(Payments.GetValueOrDefault(id));
    }
}
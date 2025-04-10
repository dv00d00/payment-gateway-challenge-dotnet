﻿using PaymentGateway.Api.Enums;
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

public class PaymentsRepository
{
    public List<StoredPayment> Payments = new();
    
    public Task<StoredPayment> AddAuthorized(PaymentDetails paymentDetails, AuthorizationCode code)
    {
        var payment = new StoredPayment.Authorized 
        { 
            Id = Guid.NewGuid(), 
            CardNumberLastFour = paymentDetails.CardNumber.LastFour, 
            ExpiryDate = ExpiryDate.FromFutureExpiryDate(paymentDetails.ExpiryDate), 
            Money = paymentDetails.Money, 
            AuthorizationCode = code.Value 
        };
        
        Payments.Add(payment);
        
        return Task.FromResult<StoredPayment>(payment);
    }
    
    public Task<StoredPayment> AddDeclined(PaymentDetails paymentDetails)
    {
        var payment = new StoredPayment.Declined 
        { 
            Id = Guid.NewGuid(), 
            CardNumberLastFour = paymentDetails.CardNumber.LastFour, 
            ExpiryDate = ExpiryDate.FromFutureExpiryDate(paymentDetails.ExpiryDate), 
            Money = paymentDetails.Money, 
        };
        
        Payments.Add(payment);
        
        return Task.FromResult<StoredPayment>(payment);
    }
    
    // todo: no tenancy
    public Task<StoredPayment?> Get(Guid id)
    {
        return Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
    }
}
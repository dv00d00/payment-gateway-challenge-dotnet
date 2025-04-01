using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

// todo: where to put?
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
    public readonly Dictionary<Guid, StoredPayment> Payments = new();
    
    public Task<StoredPayment> AddAuthorized(Guid id, PaymentDetails paymentDetails, AuthorizationCode code)
    {
        var payment = new StoredPayment.Authorized 
        { 
            Id = Guid.NewGuid(), 
            CardNumberLastFour = paymentDetails.CardNumber.LastFour, 
            ExpiryDate = ExpiryDate.FromFutureExpiryDate(paymentDetails.ExpiryDate), 
            Money = paymentDetails.Money, 
            AuthorizationCode = code.Value 
        };
        
        Payments.Add(id, payment);
        
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
        
        Payments.Add(id, payment);
        
        return Task.FromResult<StoredPayment>(payment);
    }
    
    public Task<StoredPayment?> Get(Guid id)
    {
        return Task.FromResult(Payments.GetValueOrDefault(id));
    }
}

public enum IdEnterError { HashMismatch, CantGetLock }

public record LockHandle(Guid Id, Guid LockId, DateTimeOffset ObtainedAt, TimeSpan Duration) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public abstract record IdResult
{
    public sealed record Error(IdEnterError Kind) : IdResult;
    public sealed record Cached(BankResponse BankResponse) : IdResult;
    public sealed record NoResultLockObtained(LockHandle Handle) : IdResult;
}

public class IdempotencyStore
{
    private record Stored(string Hash, BankResponse Response);

    private readonly Dictionary<Guid, Stored> _store = new();

    public async Task<IdResult> TryFindResultOrGetLock(Guid key, PaymentDetails request)
    {
        var hash = RequestHashing.ComputeHash(request);

        if (_store.TryGetValue(key, out var stored))
        {
            if (stored.Hash != hash)
                return new IdResult.Error(IdEnterError.HashMismatch);

            return new IdResult.Cached(stored.Response);
        }

        // Pretend we acquired a lock
        return new IdResult.NoResultLockObtained(new LockHandle(key, Guid.NewGuid(), DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1)));
    }

    public Task SaveResult(Guid key, PaymentDetails request, BankResponse response)
    {
        var hash = RequestHashing.ComputeHash(request);
        _store[key] = new Stored(hash, response);
        return Task.CompletedTask;
    }

    public Task ReleaseLock(LockHandle handle) => Task.CompletedTask;
}

public static class RequestHashing
{
    private const int L = 19 + // Card number
                          1 + // -
                          2 + // ExpiryMonth
                          1 + // -
                          4 + // ExpiryYear
                          1 + // -
                          4 + // CVV
                          1 + // -
                          3 + // Currency
                          1 + // -
                          10; // Amount
    
    private const int ExpectedMaximumHashStubLength = L * 2;
    
    private static readonly ObjectPool<StringBuilder> StringBuilderPool = new DefaultObjectPoolProvider()
        .CreateStringBuilderPool(ExpectedMaximumHashStubLength, ExpectedMaximumHashStubLength);
    
    public static string ComputeHash(PaymentDetails request)
    {
        var rawBytes = ArrayPool<byte>.Shared.Rent(ExpectedMaximumHashStubLength * 2);
        var rawChars = ArrayPool<char>.Shared.Rent(ExpectedMaximumHashStubLength);
        var sb = StringBuilderPool.Get();
        
        try
        {
            sb.Append(request.CardNumber.Value)
                .Append('-')
                .Append(request.ExpiryDate.Month)
                .Append('-')
                .Append(request.ExpiryDate.Year)
                .Append('-')
                .Append(request.CVV.Value)
                .Append('-')
                .Append(request.Money.Currency.Value)
                .Append('-')
                .Append(request.Money.MinorCurrencyUnitCount);
            
            sb.CopyTo(0, rawChars, sb.Length);
            var bytesCount = Encoding.UTF8.GetBytes(rawChars.AsSpan(0, sb.Length), rawBytes);
            Span<byte> hash = stackalloc byte[256 / 8];
            SHA256.HashData(rawBytes.AsSpan(0, bytesCount), hash);
            return Convert.ToHexString(hash);
        }
        finally
        {
            StringBuilderPool.Return(sb);
            ArrayPool<byte>.Shared.Return(rawBytes);
            ArrayPool<char>.Shared.Return(rawChars);
        }
    }
}
using PaymentGateway.Api.Models.Domain;

namespace PaymentGateway.Api.Services;

public enum IdempotencyCheckError { HashMismatch, LockContested }

public record LockHandle() : IAsyncDisposable
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public abstract record IdempotencyCheckResult
{
    public sealed record NoResultLockObtained(LockHandle Handle) : IdempotencyCheckResult;
    public sealed record Cached(BankResponse BankResponse) : IdempotencyCheckResult;
    public sealed record Error(IdempotencyCheckError Kind) : IdempotencyCheckResult;
}

public interface IIdempotencyStore
{
    Task<IdempotencyCheckResult> TryFindResultOrGetLock(Guid key, PaymentDetails request);
    Task SaveResultAndReleaseLock(LockHandle handle, Guid key, PaymentDetails request, BankResponse response);
}

// Persisted state machine per idempotency key
// Nothing -> Lock -> Result
// Probably dynamo with ttl on rows
public class IdempotencyStore : IIdempotencyStore
{
    private record Stored(string Hash, BankResponse Response);

    private readonly Dictionary<Guid, Stored> _store = new();

    public async Task<IdempotencyCheckResult> TryFindResultOrGetLock(Guid key, PaymentDetails request)
    {
        var hash = RequestHasher.ComputeHash(request);

        if (_store.TryGetValue(key, out var stored))
        {
            if (stored.Hash != hash)
                return new IdempotencyCheckResult.Error(IdempotencyCheckError.HashMismatch);

            return new IdempotencyCheckResult.Cached(stored.Response);
        }

        // Pretend we acquired a lock
        var handle = new LockHandle();
        return new IdempotencyCheckResult.NoResultLockObtained(handle);
    }

    public async Task SaveResultAndReleaseLock(LockHandle handle, Guid key, PaymentDetails request, BankResponse response)
    {
        await using var _ = handle;
        var hash = RequestHasher.ComputeHash(request);
        _store[key] = new Stored(hash, response);
    }
}
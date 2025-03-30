namespace PaymentGateway.Api.Types;

public record Error(string Code, string Description);

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    
    public T Value { get; }
    public IEnumerable<Error> Errors { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Errors = [];
    }

    private Result(Error error)
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        IsSuccess = false;
        Value = default!;
        Errors = [error];
    }

    private Result(IEnumerable<Error> errors)
    {
        if (errors is null)
            throw new ArgumentNullException(nameof(errors));

        Error[] arr = errors.ToArray();

        if (arr.Length == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(errors));

        IsSuccess = false;
        Value = default!;
        Errors = arr;
    }

    public static Result<T> Success(T value) => new Result<T>(value);
    public static Result<T> Failure(Error error) => new Result<T>(error);
    public static Result<T> Failure(IEnumerable<Error> errors) => new Result<T>(errors);
}

public static class ResultExtensions
{
    // Monadic bind / flatMap
    public static Result<U> Bind<T, U>(this Result<T> result, Func<T, Result<U>> func)
    {
        return result.IsSuccess
            ? func(result.Value)
            : Result<U>.Failure(result.Errors);
    }

    // LINQ Select
    public static Result<U> Select<T, U>(this Result<T> result, Func<T, Result<U>> map)
    {
        return result.IsSuccess
            ? map(result.Value)
            : Result<U>.Failure(result.Errors);
    }
    
    public static Result<U> Select<T, U>(this Result<T> result, Func<T, U> map)
    {
        return result.IsSuccess
            ? Result<U>.Success(map(result.Value))
            : Result<U>.Failure(result.Errors);
    }

    // LINQ SelectMany with error propagation
    public static Result<V> SelectMany<T, U, V>(
        this Result<T> result,
        Func<T, Result<U>> bind,
        Func<T, U, V> project)
    {
        if (!result.IsSuccess)
            return Result<V>.Failure(result.Errors);

        var intermediate = bind(result.Value);
        if (!intermediate.IsSuccess)
            return Result<V>.Failure(intermediate.Errors);

        return Result<V>.Success(project(result.Value, intermediate.Value));
    }

    // Combine two Result<T>s and accumulate errors
    public static Result<(T1, T2)> Combine<T1, T2>(Result<T1> r1, Result<T2> r2)
    {
        if (r1.IsSuccess && r2.IsSuccess)
            return Result<(T1, T2)>.Success((r1.Value, r2.Value));

        var errors = r1.Errors.Concat(r2.Errors);
        return Result<(T1, T2)>.Failure(errors);
    }

    // Combine 3
    public static Result<(T1, T2, T3)> Combine<T1, T2, T3>(Result<T1> r1, Result<T2> r2, Result<T3> r3)
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess)
            return Result<(T1, T2, T3)>.Success((r1.Value, r2.Value, r3.Value));

        var errors = r1.Errors.Concat(r2.Errors).Concat(r3.Errors);
        return Result<(T1, T2, T3)>.Failure(errors);
    }

    // Combine 4
    public static Result<(T1, T2, T3, T4)> Combine<T1, T2, T3, T4>(
        Result<T1> r1,
        Result<T2> r2,
        Result<T3> r3,
        Result<T4> r4)
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess)
            return Result<(T1, T2, T3, T4)>.Success((r1.Value, r2.Value, r3.Value, r4.Value));

        var errors = r1.Errors.Concat(r2.Errors).Concat(r3.Errors).Concat(r4.Errors);
        return Result<(T1, T2, T3, T4)>.Failure(errors);
    }

    // Combine 5
    public static Result<(T1, T2, T3, T4, T5)> Combine<T1, T2, T3, T4, T5>(
        Result<T1> r1,
        Result<T2> r2,
        Result<T3> r3,
        Result<T4> r4,
        Result<T5> r5)
    {
        if (r1.IsSuccess && r2.IsSuccess && r3.IsSuccess && r4.IsSuccess && r5.IsSuccess)
            return Result<(T1, T2, T3, T4, T5)>.Success((r1.Value, r2.Value, r3.Value, r4.Value, r5.Value));

        var errors = r1.Errors.Concat(r2.Errors).Concat(r3.Errors).Concat(r4.Errors).Concat(r5.Errors);
        return Result<(T1, T2, T3, T4, T5)>.Failure(errors);
    }
}
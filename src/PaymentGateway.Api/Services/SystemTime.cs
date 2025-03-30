namespace PaymentGateway.Api.Services;

public interface ISystemTime
{
    public DateOnly UtcToday { get; }
}

public class SystemSystemTime : ISystemTime
{
    public DateOnly UtcToday
    {
        get => DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

class FixedSystemTime(DateOnly fixedDate) : ISystemTime
{
    public DateOnly UtcToday => fixedDate;
}
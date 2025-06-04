using SharedKernel;

namespace Infrastructure.Time;

// helps for testing purposes
// you can mock this interface to return a specific date/time
internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

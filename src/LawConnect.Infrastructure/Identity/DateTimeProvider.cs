using LawConnect.Application.Common.Interfaces;

namespace LawConnect.Infrastructure.Identity;

/// <summary>
/// System-clock implementation of <see cref="IDateTimeProvider"/>.
/// </summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}

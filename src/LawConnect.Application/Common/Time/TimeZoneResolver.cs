namespace LawConnect.Application.Common.Time;

/// <summary>
/// Resolves IANA or Windows time zone identifiers with safe fallbacks.
/// </summary>
public static class TimeZoneResolver
{
    /// <summary>
    /// Resolves a time zone id, falling back to India Standard Time then UTC.
    /// </summary>
    /// <param name="timeZoneId">Requested time zone id.</param>
    /// <returns>Resolved <see cref="TimeZoneInfo"/>.</returns>
    public static TimeZoneInfo Resolve(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            try
            {
                // Windows mapping for Asia/Kolkata
                return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Utc;
            }
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}

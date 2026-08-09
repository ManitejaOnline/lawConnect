namespace LawConnect.Domain.Enums;

/// <summary>
/// Geographic SOS dispatch group. Attempt 1 = city, 2 = state, 3 = country.
/// International is opt-in and not used in MVP by default.
/// </summary>
public enum EmergencySearchScope
{
    City = 1,
    State = 2,
    Country = 3,
    International = 4
}

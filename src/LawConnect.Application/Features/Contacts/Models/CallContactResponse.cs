namespace LawConnect.Application.Features.Contacts.Models;

/// <summary>Authorized phone number for a native carrier dialer call.</summary>
public sealed record CallContactResponse(Guid UserId, string DisplayName, string PhoneNumber);

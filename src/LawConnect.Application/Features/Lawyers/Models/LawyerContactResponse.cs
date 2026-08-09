namespace LawConnect.Application.Features.Lawyers.Models;

/// <summary>Authorized phone number for a native dialer call to a verified lawyer.</summary>
/// <param name="PhoneNumber">E.164 or stored phone number from <c>User.PhoneNumber</c>.</param>
public sealed record LawyerContactResponse(string PhoneNumber);

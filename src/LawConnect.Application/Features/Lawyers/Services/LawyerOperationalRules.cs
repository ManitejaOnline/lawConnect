using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;

namespace LawConnect.Application.Features.Lawyers.Services;

/// <summary>
/// Server-side lawyer operational eligibility. Never trust client role or verified flags.
/// </summary>
public static class LawyerOperationalRules
{
    /// <summary>
    /// Returns whether the lawyer is approved for marketplace, appointments, SOS, and online presence.
    /// </summary>
    /// <param name="lawyer">Lawyer aggregate.</param>
    /// <returns><c>true</c> when approved and marked verified.</returns>
    public static bool IsApproved(Lawyer lawyer) =>
        lawyer.IsVerified && lawyer.VerificationStatus == VerificationStatus.Approved;

    /// <summary>
    /// Throws when the lawyer is not approved for operational features.
    /// </summary>
    /// <param name="lawyer">Lawyer aggregate.</param>
    /// <exception cref="ForbiddenException">Thrown when the lawyer is not approved.</exception>
    public static void EnsureApproved(Lawyer lawyer)
    {
        if (!IsApproved(lawyer))
        {
            throw new ForbiddenException("Only approved lawyers can perform this action.");
        }
    }

    /// <summary>
    /// Clears operational flags used by search, SOS, and appointments.
    /// </summary>
    /// <param name="lawyer">Lawyer aggregate.</param>
    public static void RevokeOperationalAccess(Lawyer lawyer)
    {
        lawyer.IsVerified = false;
        lawyer.IsOnline = false;
        lawyer.IsAvailableForEmergency = false;
        lawyer.IsAcceptingAppointments = false;
    }
}

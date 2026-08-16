using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Services;

/// <summary>
/// Shared "load a case and verify the caller owns it" logic, used by every case command/query
/// handler to avoid repeating the same lookup+ownership check in each one.
/// </summary>
internal static class CaseAccessHelper
{
    /// <summary>Loads a case (tracked) and verifies it belongs to the given lawyer. Throws otherwise.</summary>
    public static async Task<Case> GetOwnedByLawyerAsync(
        IApplicationDbContext dbContext,
        Guid caseId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Cases
            .FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), caseId);

        if (entity.LawyerId != lawyerId)
        {
            throw new ForbiddenException("You do not have access to this case.");
        }

        return entity;
    }

    /// <summary>Verifies the given user (lawyer or customer) may read this case.</summary>
    public static void EnsureReadAccess(Case entity, Guid lawyerUserId, Guid customerUserId, Guid userId)
    {
        if (lawyerUserId != userId && customerUserId != userId)
        {
            throw new ForbiddenException("You do not have access to this case.");
        }
    }
}

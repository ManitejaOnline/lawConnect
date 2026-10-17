using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Contacts.Models;
using LawConnect.Domain.Enums;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Contacts.Queries.GetCallContact;

/// <summary>Returns a counterpart phone number when the caller is allowed to dial them.</summary>
public sealed record GetCallContactQuery(Guid UserId) : IRequest<Result<CallContactResponse>>;

/// <summary>Validates <see cref="GetCallContactQuery"/>.</summary>
public sealed class GetCallContactQueryValidator : AbstractValidator<GetCallContactQuery>
{
    /// <summary>Initializes a new instance of the <see cref="GetCallContactQueryValidator"/> class.</summary>
    public GetCallContactQueryValidator() => RuleFor(x => x.UserId).NotEmpty();
}

/// <summary>Handles <see cref="GetCallContactQuery"/>.</summary>
public sealed class GetCallContactQueryHandler : IRequestHandler<GetCallContactQuery, Result<CallContactResponse>>
{
    private static readonly string Unavailable = "Phone number unavailable";

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="GetCallContactQueryHandler"/> class.</summary>
    public GetCallContactQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<CallContactResponse>> Handle(
        GetCallContactQuery request,
        CancellationToken cancellationToken)
    {
        var callerId = _currentUser.GetRequiredUserId();
        if (callerId == request.UserId)
        {
            return Result.Failure<CallContactResponse>(Error.NotFound(Unavailable));
        }

        var target = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);

        if (target is null || string.IsNullOrWhiteSpace(target.PhoneNumber))
        {
            return Result.Failure<CallContactResponse>(Error.NotFound(Unavailable));
        }

        var allowed = await HasCallRelationshipAsync(callerId, request.UserId, cancellationToken);
        if (!allowed)
        {
            return Result.Failure<CallContactResponse>(Error.NotFound(Unavailable));
        }

        var displayName = $"{target.FirstName} {target.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Contact";
        }

        return Result.Success(new CallContactResponse(target.Id, displayName, target.PhoneNumber.Trim()));
    }

    private async Task<bool> HasCallRelationshipAsync(
        Guid callerId,
        Guid targetUserId,
        CancellationToken cancellationToken)
    {
        // OTP assigns the Customer role immediately; the Customer entity is created lazily
        // (SOS / appointments). Marketplace Call must not wait for that entity.
        var callerIsCustomer = await _dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(
                ur => ur.UserId == callerId && ur.RoleId == SeedIds.Roles.Customer && !ur.IsDeleted,
                cancellationToken);
        var targetIsLawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .AnyAsync(l => l.UserId == targetUserId && !l.IsDeleted, cancellationToken);

        // Authenticated customers may dial a lawyer from profile / marketplace.
        // Public search and anonymous profile responses still omit phone numbers.
        if (callerIsCustomer && targetIsLawyer)
        {
            return await _dbContext.Lawyers
                .AsNoTracking()
                .AnyAsync(
                    l => l.UserId == targetUserId
                         && !l.IsDeleted
                         && l.IsVerified
                         && l.VerificationStatus == VerificationStatus.Approved
                         && l.User.IsActive,
                    cancellationToken);
        }

        var assignedSos = await _dbContext.EmergencyRequests
            .AsNoTracking()
            .AnyAsync(
                r =>
                    !r.IsDeleted &&
                    (r.Status == EmergencyStatus.Assigned ||
                     r.Status == EmergencyStatus.Calling ||
                     r.Status == EmergencyStatus.Active ||
                     r.Status == EmergencyStatus.InProgress) &&
                    r.AssignedLawyerId != null &&
                    ((r.Customer.UserId == callerId && r.AssignedLawyer!.UserId == targetUserId) ||
                     (r.AssignedLawyer!.UserId == callerId && r.Customer.UserId == targetUserId)),
                cancellationToken);
        if (assignedSos)
        {
            return true;
        }

        return await _dbContext.Appointments
            .AsNoTracking()
            .AnyAsync(
                a =>
                    !a.IsDeleted &&
                    a.Status != AppointmentStatus.Cancelled &&
                    ((a.Customer.UserId == callerId && a.Lawyer.UserId == targetUserId) ||
                     (a.Lawyer.UserId == callerId && a.Customer.UserId == targetUserId)),
                cancellationToken);
    }
}

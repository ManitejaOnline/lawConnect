using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerContact;

/// <summary>Returns a verified lawyer's phone number for an authenticated customer to dial.</summary>
/// <param name="LawyerId">Lawyer profile id from marketplace / profile (not a raw user id).</param>
public sealed record GetLawyerContactQuery(Guid LawyerId) : IRequest<Result<LawyerContactResponse>>;

/// <summary>Validates <see cref="GetLawyerContactQuery"/>.</summary>
public sealed class GetLawyerContactQueryValidator : AbstractValidator<GetLawyerContactQuery>
{
    /// <summary>Initializes a new instance of the <see cref="GetLawyerContactQueryValidator"/> class.</summary>
    public GetLawyerContactQueryValidator() => RuleFor(x => x.LawyerId).NotEmpty();
}

/// <summary>Handles <see cref="GetLawyerContactQuery"/>.</summary>
public sealed class GetLawyerContactQueryHandler : IRequestHandler<GetLawyerContactQuery, Result<LawyerContactResponse>>
{
    private static readonly string Unavailable = "Phone number unavailable";

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>Initializes a new instance of the <see cref="GetLawyerContactQueryHandler"/> class.</summary>
    public GetLawyerContactQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerContactResponse>> Handle(
        GetLawyerContactQuery request,
        CancellationToken cancellationToken)
    {
        var callerId = _currentUser.GetRequiredUserId();

        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId && !l.IsDeleted, cancellationToken);

        if (lawyer?.User is null || lawyer.User.IsDeleted)
        {
            return Result.Failure<LawyerContactResponse>(Error.NotFound(Unavailable));
        }

        if (lawyer.UserId == callerId)
        {
            return Result.Failure<LawyerContactResponse>(Error.NotFound(Unavailable));
        }

        if (!lawyer.IsVerified || lawyer.VerificationStatus != VerificationStatus.Approved)
        {
            return Result.Failure<LawyerContactResponse>(Error.NotFound(Unavailable));
        }

        if (!lawyer.User.IsActive)
        {
            return Result.Failure<LawyerContactResponse>(Error.NotFound(Unavailable));
        }

        var phone = lawyer.User.PhoneNumber?.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Result.Failure<LawyerContactResponse>(Error.NotFound(Unavailable));
        }

        return Result.Success(new LawyerContactResponse(phone));
    }
}

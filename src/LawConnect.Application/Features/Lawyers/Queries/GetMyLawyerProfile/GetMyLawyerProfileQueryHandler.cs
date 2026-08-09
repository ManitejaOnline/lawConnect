using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Lawyers.Queries.GetMyLawyerProfile;

/// <summary>
/// Handles <see cref="GetMyLawyerProfileQuery"/>.
/// </summary>
public sealed class GetMyLawyerProfileQueryHandler
    : IRequestHandler<GetMyLawyerProfileQuery, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyLawyerProfileQueryHandler"/> class.
    /// </summary>
    public GetMyLawyerProfileQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerProfileResponse>> Handle(
        GetMyLawyerProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var lawyer = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByUserIdSpec(userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Lawyer profile was not found for the current user.");

        return Result.Success(LawyerProfileMapper.ToProfileResponse(lawyer));
    }
}

using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Services;
using MediatR;

namespace LawConnect.Application.Features.Users.Queries.GetMyProfile;

/// <summary>
/// Handles <see cref="GetMyProfileQuery"/>.
/// </summary>
public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Result<UserProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetMyProfileQueryHandler"/> class.
    /// </summary>
    public GetMyProfileQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<UserProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await UserProfileHelper.GetOrCreateProfileGraphAsync(
            _dbContext,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        return Result.Success(UserProfileHelper.ToProfileResponse(user));
    }
}

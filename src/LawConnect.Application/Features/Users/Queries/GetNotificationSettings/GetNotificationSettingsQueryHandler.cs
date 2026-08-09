using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Services;
using MediatR;

namespace LawConnect.Application.Features.Users.Queries.GetNotificationSettings;

/// <summary>
/// Handles <see cref="GetNotificationSettingsQuery"/>.
/// </summary>
public sealed class GetNotificationSettingsQueryHandler
    : IRequestHandler<GetNotificationSettingsQuery, Result<NotificationSettingsResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetNotificationSettingsQueryHandler"/> class.
    /// </summary>
    public GetNotificationSettingsQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<Result<NotificationSettingsResponse>> Handle(
        GetNotificationSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await UserProfileHelper.GetOrCreateProfileGraphAsync(
            _dbContext,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        return Result.Success(_mapper.Map<NotificationSettingsResponse>(user.NotificationPreference));
    }
}

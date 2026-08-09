using AutoMapper;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Services;
using MediatR;

namespace LawConnect.Application.Features.Users.Commands.UpdateNotificationSettings;

/// <summary>
/// Handles <see cref="UpdateNotificationSettingsCommand"/>.
/// </summary>
public sealed class UpdateNotificationSettingsCommandHandler
    : IRequestHandler<UpdateNotificationSettingsCommand, Result<NotificationSettingsResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNotificationSettingsCommandHandler"/> class.
    /// </summary>
    public UpdateNotificationSettingsCommandHandler(
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
        UpdateNotificationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var user = await UserProfileHelper.GetOrCreateProfileGraphAsync(
            _dbContext,
            _currentUser.GetRequiredUserId(),
            cancellationToken);

        user.NotificationPreference!.EnablePush = request.EnablePush;
        user.NotificationPreference.EnableSms = request.EnableSms;
        user.NotificationPreference.EnableEmail = request.EnableEmail;
        user.NotificationPreference.EnableInApp = request.EnableInApp;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(_mapper.Map<NotificationSettingsResponse>(user.NotificationPreference));
    }
}

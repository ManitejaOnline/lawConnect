using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Application.Features.Users.Services;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Commands.UpdateMyProfile;

/// <summary>
/// Handles <see cref="UpdateMyProfileCommand"/>.
/// </summary>
public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Result<UserProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMyProfileCommandHandler"/> class.
    /// </summary>
    public UpdateMyProfileCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<UserProfileResponse>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var user = await UserProfileHelper.GetOrCreateProfileGraphAsync(_dbContext, userId, cancellationToken);

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (email is not null)
        {
            var emailTaken = await _dbContext.Users.AnyAsync(
                u => u.Email == email && u.Id != userId,
                cancellationToken);
            if (emailTaken)
            {
                throw new ConflictException("Email is already in use.");
            }
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.Customer!.DateOfBirth = request.DateOfBirth;
        user.Customer.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
        user.Customer.PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage)
            ? null
            : request.PreferredLanguage.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(UserProfileHelper.ToProfileResponse(user));
    }
}

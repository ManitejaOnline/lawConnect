using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.DevLogin;

/// <summary>
/// Temporary passwordless login without OTP (development / demo only).
/// </summary>
public sealed record DevLoginCommand(
    string PhoneNumber,
    string DeviceIdentifier,
    DevicePlatform Platform,
    string? FcmToken,
    string? AppVersion,
    string? OsVersion,
    string? IpAddress) : IRequest<Result<AuthLoginResponse>>;

/// <summary>
/// Validates <see cref="DevLoginCommand"/>.
/// </summary>
public sealed class DevLoginCommandValidator : AbstractValidator<DevLoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevLoginCommandValidator"/> class.
    /// </summary>
    public DevLoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DeviceIdentifier).NotEmpty().MaximumLength(200);
    }
}

/// <summary>
/// Handles temporary phone login without OTP verification.
/// </summary>
public sealed class DevLoginCommandHandler : IRequestHandler<DevLoginCommand, Result<AuthLoginResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuthTokenIssuer _authTokenIssuer;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevLoginCommandHandler"/> class.
    /// </summary>
    public DevLoginCommandHandler(IApplicationDbContext dbContext, IAuthTokenIssuer authTokenIssuer)
    {
        _dbContext = dbContext;
        _authTokenIssuer = authTokenIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<AuthLoginResponse>> Handle(
        DevLoginCommand request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);

        var isNewUser = false;
        if (user is null)
        {
            isNewUser = true;
            user = await CreatePasswordlessUserAsync(phoneNumber, cancellationToken);
            _dbContext.Notifications.AddRange(
                new Notification
                {
                    UserId = user.Id,
                    Title = "Welcome to LawConnect",
                    Body = "Browse nearby lawyers or tap SOS if you need urgent help.",
                    Type = "Welcome",
                    Channel = NotificationChannel.InApp
                },
                new Notification
                {
                    UserId = user.Id,
                    Title = "Demo data is ready",
                    Body = "Search, chat, book, and emergency SOS all have sample lawyers.",
                    Type = "System",
                    Channel = NotificationChannel.InApp
                });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (!user.IsActive)
        {
            throw new ForbiddenException("User account is disabled.");
        }
        else
        {
            user.IsPhoneVerified = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tokens = await _authTokenIssuer.IssueTokensAsync(
            user,
            roles,
            request.IpAddress,
            new DeviceUpsertRequest(
                request.DeviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion),
            cancellationToken);

        return Result.Success(new AuthLoginResponse(
            user.Id,
            user.PhoneNumber,
            roles,
            isNewUser,
            tokens));
    }

    private async Task<User> CreatePasswordlessUserAsync(
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        var customerRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == SeedIds.Roles.Customer, cancellationToken)
            ?? throw new BusinessRuleException("Customer role is not seeded.", "Auth.RoleMissing");

        var user = new User
        {
            PhoneNumber = phoneNumber,
            FirstName = "User",
            LastName = phoneNumber.Length >= 4 ? phoneNumber[^4..] : "New",
            IsActive = true,
            IsPhoneVerified = true
        };

        user.UserRoles.Add(new UserRole
        {
            User = user,
            RoleId = customerRole.Id
        });

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == user.Id, cancellationToken);
    }
}

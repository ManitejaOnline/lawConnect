using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Auth.Commands.RegisterLawyerAccount;

/// <summary>
/// Handles <see cref="RegisterLawyerAccountCommand"/>.
/// </summary>
public sealed class RegisterLawyerAccountCommandHandler
    : IRequestHandler<RegisterLawyerAccountCommand, Result<AuthLoginResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenIssuer _tokenIssuer;
    private readonly ILogger<RegisterLawyerAccountCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterLawyerAccountCommandHandler"/> class.
    /// </summary>
    public RegisterLawyerAccountCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuthTokenIssuer tokenIssuer,
        ILogger<RegisterLawyerAccountCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AuthLoginResponse>> Handle(
        RegisterLawyerAccountCommand request,
        CancellationToken cancellationToken)
    {
        var phone = request.PhoneNumber.Trim();
        var email = AuthAccountHelper.NormalizeEmail(request.Email);
        var (firstName, lastName) = AuthAccountHelper.SplitFullName(request.FullName);

        var phoneTaken = await _dbContext.Users.AnyAsync(u => u.PhoneNumber == phone, cancellationToken);
        if (phoneTaken)
        {
            throw new ConflictException("An account with this phone number already exists.");
        }

        var emailTaken = await _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var lawyerRole = await AuthAccountHelper.GetRequiredRoleAsync(
            _dbContext,
            SeedIds.Roles.Lawyer,
            "Auth.RoleMissing",
            cancellationToken);

        var user = new User
        {
            PhoneNumber = phone,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            IsPhoneVerified = false,
            IsEmailVerified = false
        };
        user.UserRoles.Add(new UserRole { User = user, RoleId = lawyerRole.Id });

        var lawyer = new Lawyer
        {
            User = user,
            VerificationStatus = VerificationStatus.Pending,
            IsVerified = false
        };
        LawyerOperationalRules.RevokeOperationalAccess(lawyer);

        _dbContext.Users.Add(user);
        _dbContext.Lawyers.Add(lawyer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Registered lawyer {LawyerId} for user {UserId} with VerificationStatus={VerificationStatus} IsVerified={IsVerified}",
            lawyer.Id,
            user.Id,
            lawyer.VerificationStatus,
            lawyer.IsVerified);

        var roles = new[] { Roles.Lawyer };
        var tokens = await _tokenIssuer.IssueTokensAsync(
            user,
            roles,
            request.IpAddress,
            AuthAccountHelper.ToDevice(
                request.DeviceIdentifier,
                request.Platform,
                request.FcmToken,
                request.AppVersion,
                request.OsVersion),
            cancellationToken);

        return Result.Success(new AuthLoginResponse(user.Id, user.PhoneNumber, roles, true, tokens));
    }
}

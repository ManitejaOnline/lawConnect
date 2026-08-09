using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.VerifyOtp;

/// <summary>
/// Handles OTP verification and passwordless login.
/// </summary>
public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<AuthLoginResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAuthTokenIssuer _authTokenIssuer;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyOtpCommandHandler"/> class.
    /// </summary>
    public VerifyOtpCommandHandler(
        IApplicationDbContext dbContext,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IAuthTokenIssuer authTokenIssuer)
    {
        _dbContext = dbContext;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _authTokenIssuer = authTokenIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<AuthLoginResponse>> Handle(
        VerifyOtpCommand request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = request.PhoneNumber.Trim();
        var utcNow = _dateTimeProvider.UtcNow;

        var otp = await _dbContext.OtpVerifications
            .Where(o =>
                o.Destination == phoneNumber
                && o.Purpose == OtpPurpose.Login
                && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            throw new UnauthorizedException("Invalid or expired OTP.");
        }

        if (otp.ExpiresAtUtc <= utcNow)
        {
            otp.IsUsed = true;
            otp.UsedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("OTP has expired.");
        }

        if (otp.AttemptCount >= otp.MaxAttempts)
        {
            otp.IsUsed = true;
            otp.UsedAtUtc = utcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException("OTP attempt limit exceeded.", "Auth.OtpMaxAttempts");
        }

        if (!_tokenHasher.Verify(request.Code.Trim(), otp.CodeHash))
        {
            otp.AttemptCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException("Invalid or expired OTP.");
        }

        otp.IsUsed = true;
        otp.UsedAtUtc = utcNow;
        otp.AttemptCount++;

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);

        var isNewUser = false;
        if (user is null)
        {
            isNewUser = true;
            user = await CreatePasswordlessUserAsync(phoneNumber, cancellationToken);
        }
        else
        {
            if (!user.IsActive)
            {
                throw new ForbiddenException("User account is disabled.");
            }

            user.IsPhoneVerified = true;
        }

        otp.UserId = user.Id;

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

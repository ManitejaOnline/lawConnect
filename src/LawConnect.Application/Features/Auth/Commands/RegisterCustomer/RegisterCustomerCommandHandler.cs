using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.RegisterCustomer;

/// <summary>
/// Handles <see cref="RegisterCustomerCommand"/>.
/// </summary>
public sealed class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Result<AuthLoginResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthTokenIssuer _tokenIssuer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterCustomerCommandHandler"/> class.
    /// </summary>
    public RegisterCustomerCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IAuthTokenIssuer tokenIssuer)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
    }

    /// <inheritdoc />
    public async Task<Result<AuthLoginResponse>> Handle(
        RegisterCustomerCommand request,
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

        var customerRole = await AuthAccountHelper.GetRequiredRoleAsync(
            _dbContext,
            SeedIds.Roles.Customer,
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
            IsEmailVerified = false,
            Customer = new Customer()
        };
        user.UserRoles.Add(new UserRole { User = user, RoleId = customerRole.Id });

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = new[] { Roles.Customer };
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

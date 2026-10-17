#pragma warning disable CS1591
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.DeleteAccount;

public sealed record DeleteAccountCommand(string? IpAddress) : IRequest<Result>;

public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public DeleteAccountCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedException("Invalid session.");

        var now = _clock.UtcNow;
        user.PhoneNumber = "d" + user.Id.ToString("N")[..16];
        user.Email = null;
        user.PasswordHash = null;
        user.FirstName = "Deleted";
        user.LastName = "Account";
        user.ProfileImageUrl = null;
        user.IsActive = false;
        user.IsPhoneVerified = false;
        user.IsEmailVerified = false;
        user.IsDeleted = true;

        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.Token = "revoked-" + token.Id.ToString("N");
            token.ReplacedByToken = null;
            token.RevokedAtUtc = now;
            token.RevokedByIp = request.IpAddress;
        }

        var devices = await _dbContext.Devices.Where(d => d.UserId == userId).ToListAsync(cancellationToken);
        foreach (var device in devices)
        {
            device.FcmToken = null;
            device.IsActive = false;
        }

        var documents = await _dbContext.CustomerDocuments
            .Where(d => d.OwnerUserId == userId && !d.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var document in documents)
        {
            document.IsDeleted = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

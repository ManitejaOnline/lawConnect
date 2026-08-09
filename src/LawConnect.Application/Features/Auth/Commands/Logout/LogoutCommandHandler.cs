using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Handles logout by revoking a refresh token.
/// </summary>
public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogoutCommandHandler"/> class.
    /// </summary>
    public LogoutCommandHandler(IApplicationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = _dateTimeProvider.UtcNow;
            token.RevokedByIp = request.IpAddress;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Auth.Queries.GetCurrentAuthUser;

/// <summary>
/// Handles <see cref="GetCurrentAuthUserQuery"/>.
/// </summary>
public sealed class GetCurrentAuthUserQueryHandler
    : IRequestHandler<GetCurrentAuthUserQuery, Result<CurrentAuthUserResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCurrentAuthUserQueryHandler"/> class.
    /// </summary>
    public GetCurrentAuthUserQueryHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result<CurrentAuthUserResponse>> Handle(
        GetCurrentAuthUserQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated
            || !Guid.TryParse(_currentUser.UserId, out var userId))
        {
            throw new UnauthorizedException();
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), userId);

        var roles = user.UserRoles
            .Where(ur => !ur.IsDeleted)
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result.Success(new CurrentAuthUserResponse(
            user.Id,
            user.PhoneNumber,
            user.FirstName,
            user.LastName,
            roles));
    }
}

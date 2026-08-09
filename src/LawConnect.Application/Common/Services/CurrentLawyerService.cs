using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Common.Services;

/// <summary>
/// Resolves the current authenticated user's lawyer profile.
/// </summary>
public interface ICurrentLawyerService
{
    /// <summary>
    /// Loads the lawyer profile for the current user or throws.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lawyer entity.</returns>
    Task<Lawyer> GetRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the lawyer id for the current user or throws.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Lawyer id.</returns>
    Task<Guid> GetRequiredIdAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="ICurrentLawyerService"/> implementation.
/// </summary>
public sealed class CurrentLawyerService : ICurrentLawyerService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentLawyerService"/> class.
    /// </summary>
    public CurrentLawyerService(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Lawyer> GetRequiredAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();
        var lawyer = await _dbContext.Lawyers
            .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

        return lawyer
            ?? throw new NotFoundException(nameof(Lawyer), userId);
    }

    /// <inheritdoc />
    public async Task<Guid> GetRequiredIdAsync(CancellationToken cancellationToken = default)
    {
        var lawyer = await GetRequiredAsync(cancellationToken);
        return lawyer.Id;
    }
}

using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Common.Services;

/// <summary>
/// Resolves or creates the customer profile for the current user.
/// </summary>
public interface ICurrentCustomerService
{
    /// <summary>
    /// Gets the customer for the current user, creating one when missing.
    /// </summary>
    Task<Customer> GetOrCreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the customer id for the current user, creating one when missing.
    /// </summary>
    Task<Guid> GetOrCreateIdAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Default <see cref="ICurrentCustomerService"/> implementation.
/// </summary>
public sealed class CurrentCustomerService : ICurrentCustomerService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="CurrentCustomerService"/> class.
    /// </summary>
    public CurrentCustomerService(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Customer> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (customer is not null)
        {
            return customer;
        }

        var userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        customer = new Customer { UserId = userId };
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    /// <inheritdoc />
    public async Task<Guid> GetOrCreateIdAsync(CancellationToken cancellationToken = default)
    {
        var customer = await GetOrCreateAsync(cancellationToken);
        return customer.Id;
    }
}

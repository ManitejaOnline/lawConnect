using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Users.Commands.DeleteAddress;

/// <summary>
/// Handles <see cref="DeleteAddressCommand"/>.
/// </summary>
public sealed class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteAddressCommandHandler"/> class.
    /// </summary>
    public DeleteAddressCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var entity = await _dbContext.Addresses
            .FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Address), request.AddressId);

        _dbContext.Addresses.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

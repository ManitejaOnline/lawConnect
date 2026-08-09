using LawConnect.Application.Common.Specifications;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Users.Specifications;

/// <summary>
/// Addresses owned by a user, primary first.
/// </summary>
public sealed class UserAddressesByUserIdSpec : SpecificationBase<Address>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserAddressesByUserIdSpec"/> class.
    /// </summary>
    /// <param name="userId">User id.</param>
    public UserAddressesByUserIdSpec(Guid userId)
        : base(a => a.UserId == userId)
    {
        ApplyOrderByDescending(a => a.CreatedAt);
    }
}

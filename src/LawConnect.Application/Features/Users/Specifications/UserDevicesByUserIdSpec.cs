using LawConnect.Application.Common.Specifications;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Users.Specifications;

/// <summary>
/// Devices owned by a user, active first.
/// </summary>
public sealed class UserDevicesByUserIdSpec : SpecificationBase<Device>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDevicesByUserIdSpec"/> class.
    /// </summary>
    /// <param name="userId">User id.</param>
    public UserDevicesByUserIdSpec(Guid userId)
        : base(d => d.UserId == userId)
    {
        ApplyOrderByDescending(d => d.CreatedAt);
    }
}

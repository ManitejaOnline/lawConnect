using AutoMapper;
using LawConnect.Application.Features.Users.Models;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Users.Mappings;

/// <summary>
/// AutoMapper profile for user management models.
/// </summary>
public sealed class UserMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserMappingProfile"/> class.
    /// </summary>
    public UserMappingProfile()
    {
        CreateMap<Address, AddressResponse>();
        CreateMap<Device, DeviceResponse>()
            .ForMember(d => d.Platform, o => o.MapFrom(s => s.Platform.ToString()));
        CreateMap<NotificationPreference, NotificationSettingsResponse>();
    }
}

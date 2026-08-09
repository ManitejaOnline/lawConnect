using AutoMapper;
using LawConnect.Application.Features.Auth.Models;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Auth.Mappings;

/// <summary>
/// AutoMapper profile for authentication models.
/// </summary>
public sealed class AuthMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthMappingProfile"/> class.
    /// </summary>
    public AuthMappingProfile()
    {
        CreateMap<User, CurrentAuthUserResponse>()
            .ForMember(
                dest => dest.UserId,
                opt => opt.MapFrom(src => src.Id))
            .ForMember(
                dest => dest.Roles,
                opt => opt.MapFrom(src =>
                    src.UserRoles.Select(ur => ur.Role.Name).Distinct().ToArray()));
    }
}

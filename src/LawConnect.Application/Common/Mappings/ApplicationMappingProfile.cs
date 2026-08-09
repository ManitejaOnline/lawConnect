using AutoMapper;

namespace LawConnect.Application.Common.Mappings;

/// <summary>
/// Marker AutoMapper profile so assembly scanning succeeds before feature profiles exist.
/// </summary>
public sealed class ApplicationMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationMappingProfile"/> class.
    /// </summary>
    public ApplicationMappingProfile()
    {
    }
}

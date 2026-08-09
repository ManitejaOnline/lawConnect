using AutoMapper;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Entities;

namespace LawConnect.Application.Features.Lawyers.Mappings;

/// <summary>
/// AutoMapper profile for lawyer models.
/// </summary>
public sealed class LawyerMappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LawyerMappingProfile"/> class.
    /// </summary>
    public LawyerMappingProfile()
    {
        CreateMap<LawyerEducation, LawyerEducationResponse>();
        CreateMap<LawyerExperience, LawyerExperienceResponse>();
        CreateMap<LawyerLanguage, LawyerLanguageResponse>();
        CreateMap<LawyerAvailability, LawyerAvailabilityResponse>()
            .ForMember(d => d.DayOfWeek, o => o.MapFrom(s => s.DayOfWeek.ToString()));
        CreateMap<LawyerDocument, LawyerDocumentResponse>()
            .ForMember(d => d.DocumentType, o => o.MapFrom(s => s.DocumentType.ToString()))
            .ForMember(d => d.VerificationStatus, o => o.MapFrom(s => s.VerificationStatus.ToString()))
            .ForMember(d => d.DownloadPath, o => o.Ignore());
    }
}

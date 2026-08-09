using FluentValidation;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.GetLawyerProfile;

/// <summary>
/// Returns a public lawyer profile by id.
/// </summary>
public sealed record GetLawyerProfileQuery(Guid LawyerId) : IRequest<Result<LawyerProfileResponse>>;

/// <summary>
/// Validates <see cref="GetLawyerProfileQuery"/>.
/// </summary>
public sealed class GetLawyerProfileQueryValidator : AbstractValidator<GetLawyerProfileQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerProfileQueryValidator"/> class.
    /// </summary>
    public GetLawyerProfileQueryValidator()
    {
        RuleFor(x => x.LawyerId).NotEmpty();
    }
}

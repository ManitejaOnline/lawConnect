using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Admin.Queries.GetLawyerApplication;

/// <summary>
/// Handles <see cref="GetLawyerApplicationQuery"/>.
/// </summary>
public sealed class GetLawyerApplicationQueryHandler
    : IRequestHandler<GetLawyerApplicationQuery, Result<AdminLawyerApplicationDetail>>
{
    private readonly IApplicationDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetLawyerApplicationQueryHandler"/> class.
    /// </summary>
    public GetLawyerApplicationQueryHandler(IApplicationDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Result<AdminLawyerApplicationDetail>> Handle(
        GetLawyerApplicationQuery request,
        CancellationToken cancellationToken)
    {
        var lawyer = await _dbContext.Lawyers
            .AsNoTracking()
            .Include(l => l.User)
            .Include(l => l.LawyerSpecializations)
            .ThenInclude(s => s.Specialization)
            .Include(l => l.Languages)
            .Include(l => l.Educations)
            .Include(l => l.Documents)
            .FirstOrDefaultAsync(l => l.Id == request.LawyerId, cancellationToken)
            ?? throw new NotFoundException("Lawyer application was not found.");

        var detail = new AdminLawyerApplicationDetail(
            lawyer.Id,
            lawyer.UserId,
            lawyer.User.FirstName,
            lawyer.User.LastName,
            lawyer.User.Email,
            lawyer.User.PhoneNumber,
            lawyer.User.ProfileImageUrl,
            lawyer.StateBarCouncil,
            lawyer.BarCouncilNumber,
            lawyer.Bio,
            lawyer.YearsOfExperience,
            lawyer.ConsultationFee,
            lawyer.CityName,
            lawyer.StateName,
            lawyer.CountryName,
            lawyer.VerificationStatus.ToString(),
            lawyer.IsVerified,
            lawyer.ApplicationSubmittedAtUtc,
            lawyer.VerifiedAtUtc,
            lawyer.ReviewReason,
            lawyer.ReviewedAtUtc,
            lawyer.CreatedAt,
            lawyer.LawyerSpecializations.Select(s => s.Specialization.Name).ToArray(),
            lawyer.Languages.Select(l => l.LanguageName).ToArray(),
            lawyer.Educations.Select(e => string.IsNullOrWhiteSpace(e.FieldOfStudy)
                ? $"{e.Degree} — {e.InstitutionName}"
                : $"{e.Degree} — {e.InstitutionName} ({e.FieldOfStudy})").ToArray(),
            lawyer.Documents.Select(d => new AdminLawyerDocumentItem(
                d.Id,
                d.DocumentType.ToString(),
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.VerificationStatus.ToString(),
                $"/api/admin/lawyers/{lawyer.Id}/documents/{d.Id}/file")).ToArray());

        return Result.Success(detail);
    }
}

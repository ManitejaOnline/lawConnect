using LawConnect.Application.Common.Extensions;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Lawyers.Specifications;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Lawyers.Commands.RegisterLawyer;

/// <summary>
/// Handles <see cref="RegisterLawyerCommand"/>.
/// </summary>
public sealed class RegisterLawyerCommandHandler : IRequestHandler<RegisterLawyerCommand, Result<LawyerProfileResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RegisterLawyerCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterLawyerCommandHandler"/> class.
    /// </summary>
    public RegisterLawyerCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<RegisterLawyerCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerProfileResponse>> Handle(
        RegisterLawyerCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();

        var existing = await _dbContext.Lawyers.AnyAsync(l => l.UserId == userId, cancellationToken);
        if (existing)
        {
            throw new ConflictException("User is already registered as a lawyer.");
        }

        var specializationIds = request.SpecializationIds.Distinct().ToArray();
        var specializations = await _dbContext.Specializations
            .Where(s => specializationIds.Contains(s.Id) && s.IsActive)
            .ToListAsync(cancellationToken);

        if (specializations.Count != specializationIds.Length)
        {
            throw new NotFoundException("One or more specializations were not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.BarCouncilNumber))
        {
            var barTaken = await _dbContext.Lawyers.AnyAsync(
                l => l.BarCouncilNumber == request.BarCouncilNumber.Trim(),
                cancellationToken);
            if (barTaken)
            {
                throw new ConflictException("Bar council number is already registered.");
            }
        }

        var lawyerRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == SeedIds.Roles.Lawyer, cancellationToken)
            ?? throw new BusinessRuleException("Lawyer role is not seeded.", "Auth.RoleMissing");

        var hasLawyerRole = await _dbContext.UserRoles.AnyAsync(
            ur => ur.UserId == userId && ur.RoleId == lawyerRole.Id && !ur.IsDeleted,
            cancellationToken);
        if (!hasLawyerRole)
        {
            _dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = lawyerRole.Id });
        }

        // Lawyers must not retain Customer — CustomerOnly / LawyerOnly policies are exclusive.
        var customerLinks = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId && ur.RoleId == SeedIds.Roles.Customer && !ur.IsDeleted)
            .ToListAsync(cancellationToken);
        if (customerLinks.Count > 0)
        {
            _dbContext.UserRoles.RemoveRange(customerLinks);
        }

        var lawyer = new Lawyer
        {
            UserId = userId,
            BarCouncilNumber = string.IsNullOrWhiteSpace(request.BarCouncilNumber) ? null : request.BarCouncilNumber.Trim(),
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            YearsOfExperience = request.YearsOfExperience,
            ConsultationFee = request.ConsultationFee,
            EmergencyFee = request.EmergencyFee,
            VerificationStatus = LawConnect.Domain.Enums.VerificationStatus.Pending,
            IsVerified = false
        };
        LawyerOperationalRules.RevokeOperationalAccess(lawyer);

        for (var i = 0; i < specializationIds.Length; i++)
        {
            lawyer.LawyerSpecializations.Add(new LawyerSpecialization
            {
                SpecializationId = specializationIds[i],
                YearsOfExperience = request.YearsOfExperience,
                IsPrimary = i == 0
            });
        }

        _dbContext.Lawyers.Add(lawyer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var loaded = await _dbContext.Lawyers
            .ApplySpecification(new LawyerByIdWithDetailsSpec(lawyer.Id))
            .FirstAsync(cancellationToken);

        _logger.LogInformation("Registered lawyer {LawyerId} for user {UserId}", lawyer.Id, userId);
        return Result.Success(LawyerProfileMapper.ToProfileResponse(loaded));
    }
}

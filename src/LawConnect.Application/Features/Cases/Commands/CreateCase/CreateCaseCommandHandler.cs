using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Cases.Commands.CreateCase;

/// <summary>
/// Handles <see cref="CreateCaseCommand"/>.
/// </summary>
public sealed class CreateCaseCommandHandler : IRequestHandler<CreateCaseCommand, Result<CaseResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<CreateCaseCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCaseCommandHandler"/> class.
    /// </summary>
    public CreateCaseCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IDateTimeProvider clock,
        ILogger<CreateCaseCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<CaseResponse>> Handle(CreateCaseCommand request, CancellationToken cancellationToken)
    {
        var lawyer = await _currentLawyer.GetRequiredAsync(cancellationToken);

        var customer = await _dbContext.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var specialization = await _dbContext.Specializations
            .FirstOrDefaultAsync(s => s.Id == request.SpecializationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Specialization), request.SpecializationId);

        var hasRelationship = await _dbContext.Appointments.AnyAsync(
            a => a.LawyerId == lawyer.Id && a.CustomerId == request.CustomerId && a.Status != AppointmentStatus.Cancelled,
            cancellationToken);
        if (!hasRelationship)
        {
            throw new BusinessRuleException(
                "A case can only be created for a customer you have an appointment relationship with.",
                "Case.NoRelationship");
        }

        if (request.SourceAppointmentId is { } sourceAppointmentId)
        {
            var sourceValid = await _dbContext.Appointments.AnyAsync(
                a => a.Id == sourceAppointmentId && a.LawyerId == lawyer.Id && a.CustomerId == request.CustomerId,
                cancellationToken);
            if (!sourceValid)
            {
                throw new BusinessRuleException("Source appointment does not belong to this lawyer/customer pair.", "Case.InvalidSourceAppointment");
            }
        }

        var entity = new Case
        {
            CustomerId = request.CustomerId,
            LawyerId = lawyer.Id,
            SpecializationId = request.SpecializationId,
            Title = request.Title.Trim(),
            CaseNumber = string.IsNullOrWhiteSpace(request.CaseNumber) ? null : request.CaseNumber.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CourtName = string.IsNullOrWhiteSpace(request.CourtName) ? null : request.CourtName.Trim(),
            Status = CaseStatus.Active,
            Priority = request.Priority,
            StartDate = request.StartDate,
            SourceAppointmentId = request.SourceAppointmentId
        };

        _dbContext.Cases.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Lawyer {LawyerId} created case {CaseId} for customer {CustomerId}", lawyer.Id, entity.Id, request.CustomerId);

        var saved = await _dbContext.Cases
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .FirstAsync(c => c.Id == entity.Id, cancellationToken);
        return Result.Success(CaseMapper.ToResponse(saved));
    }
}

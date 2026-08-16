using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Admin.Models;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Admin.Commands.VerifyComplaint;

/// <summary>
/// Handles <see cref="VerifyComplaintCommand"/>.
/// </summary>
public sealed class VerifyComplaintCommandHandler
    : IRequestHandler<VerifyComplaintCommand, Result<AdminComplaintSummary>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ITrustScoreService _trustScoreService;
    private readonly ILogger<VerifyComplaintCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyComplaintCommandHandler"/> class.
    /// </summary>
    public VerifyComplaintCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        ITrustScoreService trustScoreService,
        ILogger<VerifyComplaintCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
        _trustScoreService = trustScoreService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AdminComplaintSummary>> Handle(VerifyComplaintCommand request, CancellationToken cancellationToken)
    {
        var complaint = await _dbContext.LawyerComplaints
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Client).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == request.ComplaintId, cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerComplaint), request.ComplaintId);

        if (complaint.Status != ComplaintStatus.Pending)
        {
            throw new ConflictException("Only a pending complaint can be verified.");
        }

        var adminId = _currentUser.GetRequiredUserId();
        var now = _clock.UtcNow;

        complaint.Status = ComplaintStatus.Verified;
        complaint.Severity = request.Severity;
        complaint.AdminNotes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        complaint.ReviewedByUserId = adminId;
        complaint.ReviewedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin {AdminId} verified complaint {ComplaintId} against lawyer {LawyerId} with severity {Severity}",
            adminId,
            complaint.Id,
            complaint.LawyerId,
            request.Severity);

        await _trustScoreService.RecalculateAsync(complaint.LawyerId, cancellationToken);

        return Result.Success(new AdminComplaintSummary(
            complaint.Id,
            complaint.LawyerId,
            $"{complaint.Lawyer.User.FirstName} {complaint.Lawyer.User.LastName}".Trim(),
            complaint.ClientId,
            $"{complaint.Client.User.FirstName} {complaint.Client.User.LastName}".Trim(),
            complaint.AppointmentId,
            complaint.Category,
            complaint.Description,
            complaint.Status.ToString(),
            complaint.Severity?.ToString(),
            complaint.AdminNotes,
            complaint.ReviewedAtUtc,
            complaint.CreatedAt));
    }
}

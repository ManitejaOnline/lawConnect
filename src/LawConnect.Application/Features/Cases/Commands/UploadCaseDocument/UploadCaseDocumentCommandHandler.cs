using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using LawConnect.Shared.Options;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LawConnect.Application.Features.Cases.Commands.UploadCaseDocument;

/// <summary>
/// Handles <see cref="UploadCaseDocumentCommand"/>. Either the case's lawyer or its customer may
/// upload — every successful upload also logs a <see cref="CaseUpdate"/> activity entry so it
/// shows up in the timeline and, for lawyer uploads only, counts as meaningful lawyer activity.
/// </summary>
public sealed class UploadCaseDocumentCommandHandler : IRequestHandler<UploadCaseDocumentCommand, Result<CaseDocumentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IFileStorage _fileStorage;
    private readonly StorageOptions _storageOptions;
    private readonly ITrustScoreService _trustScoreService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadCaseDocumentCommandHandler"/> class.
    /// </summary>
    public UploadCaseDocumentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICurrentLawyerService currentLawyer,
        ICurrentCustomerService currentCustomer,
        IFileStorage fileStorage,
        IOptions<StorageOptions> storageOptions,
        ITrustScoreService trustScoreService)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _currentLawyer = currentLawyer;
        _currentCustomer = currentCustomer;
        _fileStorage = fileStorage;
        _storageOptions = storageOptions.Value;
        _trustScoreService = trustScoreService;
    }

    /// <inheritdoc />
    public async Task<Result<CaseDocumentResponse>> Handle(UploadCaseDocumentCommand request, CancellationToken cancellationToken)
    {
        if (request.FileSizeBytes > _storageOptions.MaxUploadBytes)
        {
            throw new BusinessRuleException($"File exceeds maximum size of {_storageOptions.MaxUploadBytes} bytes.", "Storage.FileTooLarge");
        }

        var caseEntity = await _dbContext.Cases
            .Include(c => c.Lawyer)
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Case), request.CaseId);

        bool isLawyer;
        Guid uploaderUserId;
        string actorLabel;

        if (_currentUser.Roles.Contains(Roles.Lawyer))
        {
            var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
            if (caseEntity.LawyerId != lawyerId)
            {
                throw new ForbiddenException("You do not have access to this case.");
            }

            isLawyer = true;
            uploaderUserId = caseEntity.Lawyer.UserId;
            actorLabel = "Lawyer";
        }
        else if (_currentUser.Roles.Contains(Roles.Customer))
        {
            var customerId = await _currentCustomer.GetOrCreateIdAsync(cancellationToken);
            if (caseEntity.CustomerId != customerId)
            {
                throw new ForbiddenException("You do not have access to this case.");
            }

            isLawyer = false;
            uploaderUserId = _currentUser.GetRequiredUserId();
            actorLabel = "Customer";
        }
        else
        {
            throw new ForbiddenException("Only the case's lawyer or customer can upload documents.");
        }

        if (request.CaseUpdateId is { } caseUpdateId)
        {
            var exists = await _dbContext.CaseUpdates.AnyAsync(u => u.Id == caseUpdateId && u.CaseId == request.CaseId, cancellationToken);
            if (!exists) throw new NotFoundException(nameof(CaseUpdate), caseUpdateId);
        }

        if (request.HearingId is { } hearingId)
        {
            var exists = await _dbContext.Hearings.AnyAsync(h => h.Id == hearingId && h.CaseId == request.CaseId, cancellationToken);
            if (!exists) throw new NotFoundException(nameof(Hearing), hearingId);
        }

        if (request.MilestoneId is { } milestoneId)
        {
            var exists = await _dbContext.CaseMilestones.AnyAsync(m => m.Id == milestoneId && m.CaseId == request.CaseId, cancellationToken);
            if (!exists) throw new NotFoundException(nameof(CaseMilestone), milestoneId);
        }

        var storageKey = await _fileStorage.UploadAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            $"private/cases/{request.CaseId:D}/documents",
            cancellationToken);

        var fileName = request.FileName.Trim();

        _dbContext.CaseUpdates.Add(new CaseUpdate
        {
            CaseId = request.CaseId,
            CreatedByUserId = uploaderUserId,
            UpdateType = isLawyer ? CaseUpdateType.DocumentFiled : CaseUpdateType.DocumentReceived,
            Title = "Document uploaded",
            Description = $"{actorLabel} uploaded {fileName}"
        });

        var entity = new CaseDocument
        {
            CaseId = request.CaseId,
            CaseUpdateId = request.CaseUpdateId,
            HearingId = request.HearingId,
            MilestoneId = request.MilestoneId,
            UploadedByUserId = uploaderUserId,
            FileName = fileName,
            StorageKey = storageKey,
            ContentType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes
        };

        _dbContext.CaseDocuments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (isLawyer)
        {
            await _trustScoreService.RecalculateAsync(caseEntity.LawyerId, cancellationToken);
        }

        return Result.Success(CaseMapper.ToResponse(entity));
    }
}

#pragma warning disable CS1591
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Realtime;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Documents;

/// <summary>Loads customer documents and checks the caller against the linked case or appointment.</summary>
public interface IDocumentAccess
{
    Task<CustomerDocument> RegisterAsync(Guid? caseId, Guid? appointmentId, string fileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerDocumentResponse>> ListAsync(Guid? caseId, Guid? appointmentId, CancellationToken cancellationToken);

    Task<DocumentTransferSession> RequestAsync(Guid documentId, CancellationToken cancellationToken);

    Task<ApprovedTransfer> ApproveAsync(Guid sessionId, CancellationToken cancellationToken);

    Task RejectAsync(Guid sessionId, CancellationToken cancellationToken);

    Task<DocumentTransferSession> BeginUploadAsync(Guid sessionId, Guid userId, string token, CancellationToken cancellationToken);

    Task<DocumentTransferSession> BeginDownloadAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken);

    Task CompleteAsync(Guid sessionId, long bytes, CancellationToken cancellationToken);

    Task InterruptAsync(Guid sessionId, CancellationToken cancellationToken);
}

/// <summary>Metadata returned to either party. Local paths are never included.</summary>
public sealed record CustomerDocumentResponse(
    Guid DocumentId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    Guid? CaseId,
    Guid? AppointmentId,
    DateTime CreatedAtUtc,
    Guid? OpenTransferId,
    string? OpenTransferStatus);

/// <summary>Single-use upload token returned only to the document owner.</summary>
public sealed record ApprovedTransfer(Guid SessionId, string UploadToken, DateTime ExpiresAtUtc);

/// <inheritdoc cref="IDocumentAccess" />
public sealed class DocumentAccess : IDocumentAccess
{
    private static readonly DocumentTransferStatus[] OpenStatuses =
    [
        DocumentTransferStatus.Pending,
        DocumentTransferStatus.Approved,
        DocumentTransferStatus.Transferring
    ];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IPresenceTracker _presence;
    private readonly ITokenHasher _hasher;
    private readonly IUserNotificationDispatcher _notifications;
    private readonly IDocumentTransferNotifier _notifier;

    public DocumentAccess(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IPresenceTracker presence,
        ITokenHasher hasher,
        IUserNotificationDispatcher notifications,
        IDocumentTransferNotifier notifier)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _presence = presence;
        _hasher = hasher;
        _notifications = notifications;
        _notifier = notifier;
    }

    /// <inheritdoc />
    public async Task<CustomerDocument> RegisterAsync(
        Guid? caseId,
        Guid? appointmentId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        if (fileSizeBytes <= 0 || fileSizeBytes > CustomerDocumentRules.MaxBytes)
        {
            throw new BusinessRuleException($"Documents must be between 1 byte and {CustomerDocumentRules.MaxBytes} bytes.", "Document.InvalidSize");
        }

        contentType = contentType.Trim().ToLowerInvariant();
        if (contentType == "image/jpg")
        {
            contentType = "image/jpeg";
        }

        if (!CustomerDocumentRules.IsAllowedContentType(contentType))
        {
            throw new BusinessRuleException("This file type is not allowed.", "Document.InvalidType");
        }

        await EnsureCustomerContextAsync(userId, caseId, appointmentId, cancellationToken);
        var document = new CustomerDocument
        {
            OwnerUserId = userId,
            CaseId = caseId,
            AppointmentId = appointmentId,
            FileName = CustomerDocumentRules.SanitizeFileName(fileName),
            ContentType = contentType.Trim().ToLowerInvariant(),
            FileSizeBytes = fileSizeBytes
        };
        _db.CustomerDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);
        return document;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CustomerDocumentResponse>> ListAsync(
        Guid? caseId,
        Guid? appointmentId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        if (caseId.HasValue == appointmentId.HasValue)
        {
            throw new BusinessRuleException("Provide a case or an appointment.", "Document.ContextRequired");
        }

        await EnsurePartyAsync(userId, caseId, appointmentId, cancellationToken);
        var query = _db.CustomerDocuments.AsNoTracking().Where(d => !d.IsDeleted);
        query = caseId.HasValue
            ? query.Where(d => d.CaseId == caseId)
            : query.Where(d => d.AppointmentId == appointmentId);
        var documents = await query.OrderByDescending(d => d.CreatedAt).ToListAsync(cancellationToken);
        var ids = documents.Select(d => d.Id).ToList();
        var sessions = await _db.DocumentTransferSessions.Where(s => ids.Contains(s.DocumentId)).ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            await ExpireAsync(session, cancellationToken);
        }

        return documents.Select(d =>
        {
            var open = sessions
                .Where(s => s.DocumentId == d.Id && OpenStatuses.Contains(s.Status) && (s.LawyerUserId == userId || d.OwnerUserId == userId))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            return new CustomerDocumentResponse(
                d.Id,
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.CaseId,
                d.AppointmentId,
                d.CreatedAt,
                open?.Id,
                open?.Status.ToString());
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<DocumentTransferSession> RequestAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var lawyerUserId = _currentUser.GetRequiredUserId();
        var document = await LoadDocumentAsync(documentId, cancellationToken);
        await EnsureLawyerAsync(document, lawyerUserId, cancellationToken);
        if (!_presence.IsOnline(document.OwnerUserId))
        {
            throw new ConflictException("The customer is offline. Ask again when their app is open.");
        }

        var existing = await _db.DocumentTransferSessions
            .Where(s => s.DocumentId == document.Id)
            .ToListAsync(cancellationToken);
        foreach (var session in existing)
        {
            await ExpireAsync(session, cancellationToken);
        }

        if (existing.Any(s => OpenStatuses.Contains(s.Status)))
        {
            throw new ConflictException("A transfer for this document is already open.");
        }

        var created = new DocumentTransferSession
        {
            DocumentId = document.Id,
            LawyerUserId = lawyerUserId,
            Status = DocumentTransferStatus.Pending,
            ExpiresAtUtc = _clock.UtcNow.Add(CustomerDocumentRules.PendingLifetime)
        };
        _db.DocumentTransferSessions.Add(created);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new ConflictException("A transfer for this document is already open.");
        }

        var notice = new DocumentTransferNotice(created.Id, document.Id, document.FileName);
        await _notifications.NotifyUserAsync(
            document.OwnerUserId,
            "Document request",
            $"A lawyer asked to receive {document.FileName}. Open the app to approve it.",
            Shared.Constants.NotificationTypes.DocumentTransferRequested,
            new Dictionary<string, string> { ["sessionId"] = created.Id.ToString("D"), ["documentId"] = document.Id.ToString("D") },
            cancellationToken);
        await _notifier.RequestedAsync(document.OwnerUserId, notice, cancellationToken);
        return created;
    }

    /// <inheritdoc />
    public async Task<ApprovedTransfer> ApproveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        await ExpireAsync(session, cancellationToken);
        if (session.Document.OwnerUserId != userId)
        {
            throw new ForbiddenException("Only the document owner can approve a transfer.");
        }

        if (!_presence.IsOnline(userId))
        {
            throw new ConflictException("You appear to be offline. Keep the app open and try again.");
        }

        if (session.Status != DocumentTransferStatus.Pending)
        {
            throw new ConflictException("This request can no longer be approved.");
        }

        var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        session.UploadTokenHash = _hasher.Hash(token);
        session.Status = DocumentTransferStatus.Approved;
        session.ExpiresAtUtc = _clock.UtcNow.Add(CustomerDocumentRules.ApprovedLifetime);
        await _db.SaveChangesAsync(cancellationToken);
        await _notifier.ReadyAsync(
            session.LawyerUserId,
            new DocumentTransferNotice(session.Id, session.DocumentId, session.Document.FileName),
            cancellationToken);
        return new ApprovedTransfer(session.Id, token, session.ExpiresAtUtc);
    }

    /// <inheritdoc />
    public async Task RejectAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        if (session.Document.OwnerUserId != userId)
        {
            throw new ForbiddenException("Only the document owner can reject a transfer.");
        }

        if (session.Status != DocumentTransferStatus.Pending)
        {
            throw new ConflictException("This request can no longer be rejected.");
        }

        session.Status = DocumentTransferStatus.Rejected;
        session.UploadTokenHash = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DocumentTransferSession> BeginUploadAsync(Guid sessionId, Guid userId, string token, CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        await ExpireAsync(session, cancellationToken);
        if (session.Document.OwnerUserId != userId)
        {
            throw new ForbiddenException("Only the document owner can send this file.");
        }

        if (session.Status == DocumentTransferStatus.Expired)
        {
            throw new ConflictException("This transfer approval has expired.");
        }

        if (session.Status != DocumentTransferStatus.Approved || string.IsNullOrWhiteSpace(session.UploadTokenHash) || !_hasher.Verify(token, session.UploadTokenHash))
        {
            throw new ForbiddenException("The transfer token is invalid or already used.");
        }

        if (!_presence.IsOnline(userId))
        {
            throw new ConflictException("You appear to be offline. Keep the app open and try again.");
        }

        session.Status = DocumentTransferStatus.Transferring;
        session.UploadTokenHash = null;
        session.StartedAtUtc = _clock.UtcNow;
        session.ExpiresAtUtc = _clock.UtcNow.Add(CustomerDocumentRules.RelayWait);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ForbiddenException("The transfer token is invalid or already used.");
        }

        return session;
    }

    /// <inheritdoc />
    public async Task<DocumentTransferSession> BeginDownloadAsync(Guid sessionId, Guid userId, CancellationToken cancellationToken)
    {
        var session = await LoadSessionAsync(sessionId, cancellationToken);
        await ExpireAsync(session, cancellationToken);
        if (session.LawyerUserId != userId)
        {
            throw new ForbiddenException("Only the lawyer who requested this document can download it.");
        }

        await EnsureLawyerAsync(session.Document, userId, cancellationToken);
        if (session.Status is not (DocumentTransferStatus.Approved or DocumentTransferStatus.Transferring))
        {
            throw new ConflictException("The customer has not approved this transfer.");
        }

        if (!_presence.IsOnline(session.Document.OwnerUserId))
        {
            throw new ConflictException("The customer is offline, so the file cannot be sent.");
        }

        return session;
    }

    /// <inheritdoc />
    public async Task CompleteAsync(Guid sessionId, long bytes, CancellationToken cancellationToken)
    {
        var session = await _db.DocumentTransferSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null || session.Status == DocumentTransferStatus.Completed)
        {
            return;
        }

        session.Status = DocumentTransferStatus.Completed;
        session.BytesTransferred = bytes;
        session.CompletedAtUtc = _clock.UtcNow;
        session.UploadTokenHash = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task InterruptAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _db.DocumentTransferSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session is null || session.Status is DocumentTransferStatus.Completed or DocumentTransferStatus.Interrupted)
        {
            return;
        }

        session.Status = DocumentTransferStatus.Interrupted;
        session.UploadTokenHash = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;
        return message.Contains("23505", StringComparison.Ordinal)
            || message.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ExpireAsync(DocumentTransferSession session, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (!OpenStatuses.Contains(session.Status) || session.ExpiresAtUtc > _clock.UtcNow)
        {
            return;
        }

        session.Status = DocumentTransferStatus.Expired;
        session.UploadTokenHash = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustomerDocument> LoadDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _db.CustomerDocuments
            .Include(d => d.Case!).ThenInclude(c => c.Customer)
            .Include(d => d.Case!).ThenInclude(c => c.Lawyer)
            .Include(d => d.Appointment!).ThenInclude(a => a.Customer)
            .Include(d => d.Appointment!).ThenInclude(a => a.Lawyer)
            .FirstOrDefaultAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerDocument), documentId);
        return document;
    }

    private async Task<DocumentTransferSession> LoadSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _db.DocumentTransferSessions
            .Include(s => s.Document).ThenInclude(d => d.Case!).ThenInclude(c => c.Customer)
            .Include(s => s.Document).ThenInclude(d => d.Case!).ThenInclude(c => c.Lawyer)
            .Include(s => s.Document).ThenInclude(d => d.Appointment!).ThenInclude(a => a.Customer)
            .Include(s => s.Document).ThenInclude(d => d.Appointment!).ThenInclude(a => a.Lawyer)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(DocumentTransferSession), sessionId);
        return session;
    }

    private async Task EnsureCustomerContextAsync(Guid userId, Guid? caseId, Guid? appointmentId, CancellationToken cancellationToken)
    {
        if (caseId.HasValue == appointmentId.HasValue)
        {
            throw new BusinessRuleException("Link the document to one case or one appointment.", "Document.ContextRequired");
        }

        if (caseId.HasValue)
        {
            var matter = await _db.Cases.Include(c => c.Customer).FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Case), caseId.Value);
            if (matter.Customer.UserId != userId)
            {
                throw new ForbiddenException("Only the customer on this case can add a phone document.");
            }

            return;
        }

        var appointment = await _db.Appointments.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId!.Value);
        if (appointment.Customer.UserId != userId)
        {
            throw new ForbiddenException("Only the customer on this appointment can add a phone document.");
        }
    }

    private async Task EnsurePartyAsync(Guid userId, Guid? caseId, Guid? appointmentId, CancellationToken cancellationToken)
    {
        if (caseId.HasValue)
        {
            var matter = await _db.Cases.Include(c => c.Customer).Include(c => c.Lawyer).FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Case), caseId.Value);
            if (matter.Customer.UserId != userId && matter.Lawyer.UserId != userId)
            {
                throw new ForbiddenException("You are not a party to this case.");
            }

            return;
        }

        var appointment = await _db.Appointments.Include(a => a.Customer).Include(a => a.Lawyer)
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId!.Value);
        if (appointment.Customer.UserId != userId && appointment.Lawyer.UserId != userId)
        {
            throw new ForbiddenException("You are not a party to this appointment.");
        }
    }

    private static Task EnsureLawyerAsync(CustomerDocument document, Guid lawyerUserId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var allowed = (document.Case is not null && document.Case.Lawyer.UserId == lawyerUserId)
            || (document.Appointment is not null && document.Appointment.Lawyer.UserId == lawyerUserId);
        if (!allowed)
        {
            throw new ForbiddenException("Only the lawyer on this case or appointment can request the document.");
        }

        return Task.CompletedTask;
    }
}

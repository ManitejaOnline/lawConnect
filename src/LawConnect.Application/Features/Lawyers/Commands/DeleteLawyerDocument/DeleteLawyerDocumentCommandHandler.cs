using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Lawyers.Commands.DeleteLawyerDocument;

/// <summary>
/// Handles <see cref="DeleteLawyerDocumentCommand"/>.
/// </summary>
public sealed class DeleteLawyerDocumentCommandHandler : IRequestHandler<DeleteLawyerDocumentCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<DeleteLawyerDocumentCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteLawyerDocumentCommandHandler"/> class.
    /// </summary>
    public DeleteLawyerDocumentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentLawyerService currentLawyer,
        IFileStorage fileStorage,
        ILogger<DeleteLawyerDocumentCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentLawyer = currentLawyer;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(DeleteLawyerDocumentCommand request, CancellationToken cancellationToken)
    {
        var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
        var document = await _dbContext.LawyerDocuments.FirstOrDefaultAsync(
            d => d.Id == request.DocumentId && d.LawyerId == lawyerId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(LawyerDocument), request.DocumentId);

        await _fileStorage.DeleteAsync(document.StorageKey, cancellationToken);
        _dbContext.LawyerDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted document {DocumentId} for lawyer {LawyerId}", document.Id, lawyerId);
        return Result.Success();
    }
}

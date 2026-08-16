using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Cases.Models;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Cases.Queries.ListCases;

/// <summary>
/// Handles <see cref="ListCasesQuery"/>.
/// </summary>
public sealed class ListCasesQueryHandler : IRequestHandler<ListCasesQuery, Result<PagedResult<CaseListItemResponse>>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentLawyerService _currentLawyer;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IDateTimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListCasesQueryHandler"/> class.
    /// </summary>
    public ListCasesQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        ICurrentLawyerService currentLawyer,
        ICurrentCustomerService currentCustomer,
        IDateTimeProvider clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _currentLawyer = currentLawyer;
        _currentCustomer = currentCustomer;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<CaseListItemResponse>>> Handle(ListCasesQuery request, CancellationToken cancellationToken)
    {
        var isLawyer = _currentUser.Roles.Contains(Roles.Lawyer);

        var query = _dbContext.Cases
            .Include(c => c.Customer).ThenInclude(c => c.User)
            .Include(c => c.Lawyer).ThenInclude(l => l.User)
            .Include(c => c.Specialization)
            .AsQueryable();

        if (isLawyer)
        {
            var lawyerId = await _currentLawyer.GetRequiredIdAsync(cancellationToken);
            query = query.Where(c => c.LawyerId == lawyerId);
        }
        else if (_currentUser.Roles.Contains(Roles.Customer))
        {
            var customerId = await _currentCustomer.GetOrCreateIdAsync(cancellationToken);
            query = query.Where(c => c.CustomerId == customerId);
        }
        else
        {
            throw new ForbiddenException("Only lawyers and customers can list cases.");
        }

        if (request.Status is { } status)
        {
            query = query.Where(c => c.Status == status);
        }

        query = query.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var page = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var caseIds = page.Select(c => c.Id).ToList();
        var hearings = await _dbContext.Hearings.Where(h => caseIds.Contains(h.CaseId)).ToListAsync(cancellationToken);
        var milestones = await _dbContext.CaseMilestones.Where(m => caseIds.Contains(m.CaseId)).ToListAsync(cancellationToken);
        var lastUpdateByCase = await _dbContext.CaseUpdates
            .Where(u => caseIds.Contains(u.CaseId))
            .GroupBy(u => u.CaseId)
            .Select(g => new { CaseId = g.Key, Last = g.Max(u => u.CreatedAt) })
            .ToListAsync(cancellationToken);

        var now = _clock.UtcNow;
        var items = page.Select(c =>
        {
            var caseHearings = hearings.Where(h => h.CaseId == c.Id).ToList();
            var caseMilestones = milestones.Where(m => m.CaseId == c.Id).ToList();
            var upcoming = CaseActivityCalculator.ComputeUpcomingAction(caseHearings, caseMilestones, now);
            var lastUpdateAt = lastUpdateByCase.FirstOrDefault(x => x.CaseId == c.Id)?.Last;
            var otherPartyName = isLawyer
                ? $"{c.Customer.User.FirstName} {c.Customer.User.LastName}".Trim()
                : $"{c.Lawyer.User.FirstName} {c.Lawyer.User.LastName}".Trim();

            return new CaseListItemResponse(
                c.Id,
                c.Title,
                c.CaseNumber,
                otherPartyName,
                c.Specialization.Name,
                c.Status.ToString(),
                c.Priority.ToString(),
                c.StartDate,
                upcoming,
                lastUpdateAt,
                lastUpdateAt.HasValue ? (int)(now - lastUpdateAt.Value).TotalDays : null);
        }).ToList();

        return Result.Success(new PagedResult<CaseListItemResponse>(items, totalCount, request.PageNumber, request.PageSize));
    }
}

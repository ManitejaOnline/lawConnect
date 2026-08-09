#pragma warning disable CS1591
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Features.Payments.Models;
using LawConnect.Application.Features.Payments.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Payments.Queries;

/// <summary>Gets current wallet balance.</summary>
public sealed record GetMyWalletQuery : IRequest<Result<WalletResponse>>;

public sealed class GetMyWalletQueryHandler : IRequestHandler<GetMyWalletQuery, Result<WalletResponse>>
{
    private readonly IPaymentService _payments;
    private readonly ICurrentUser _currentUser;

    public GetMyWalletQueryHandler(IPaymentService payments, ICurrentUser currentUser)
    {
        _payments = payments;
        _currentUser = currentUser;
    }

    public async Task<Result<WalletResponse>> Handle(GetMyWalletQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _payments.GetOrCreateWalletAsync(_currentUser.GetRequiredUserId(), cancellationToken);
        return Result.Success(new WalletResponse(wallet.Id, wallet.AvailableBalance, wallet.HeldBalance, wallet.Currency));
    }
}

/// <summary>Lists wallet transactions.</summary>
public sealed record GetMyWalletTransactionsQuery(int PageNumber, int PageSize)
    : IRequest<Result<PagedResult<WalletTransactionResponse>>>;

public sealed class GetMyWalletTransactionsQueryHandler
    : IRequestHandler<GetMyWalletTransactionsQuery, Result<PagedResult<WalletTransactionResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyWalletTransactionsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<WalletTransactionResponse>>> Handle(
        GetMyWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var pagination = new PaginationRequest { PageNumber = request.PageNumber, PageSize = request.PageSize };
        var walletId = await _db.Wallets.AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (walletId is null)
        {
            return Result.Success(new PagedResult<WalletTransactionResponse>([], 0, pagination.PageNumber, pagination.PageSize));
        }

        var query = _db.WalletTransactions.AsNoTracking().Where(t => t.WalletId == walletId.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(t => new WalletTransactionResponse(
                t.Id,
                t.Type.ToString(),
                t.Amount,
                t.AvailableBalanceAfter,
                t.HeldBalanceAfter,
                t.Description,
                t.CreatedAt,
                t.ReferenceType,
                t.ReferenceId))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<WalletTransactionResponse>(items, total, pagination.PageNumber, pagination.PageSize));
    }
}

/// <summary>Gets invoice by payment id.</summary>
public sealed record GetInvoiceByPaymentQuery(Guid PaymentId) : IRequest<Result<InvoiceResponse>>;

public sealed class GetInvoiceByPaymentQueryHandler : IRequestHandler<GetInvoiceByPaymentQuery, Result<InvoiceResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetInvoiceByPaymentQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<InvoiceResponse>> Handle(GetInvoiceByPaymentQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var invoice = await _db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.PaymentId == request.PaymentId && i.UserId == userId, cancellationToken)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(Domain.Entities.Invoice), request.PaymentId);

        return Result.Success(new InvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.PaymentId,
            invoice.Amount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.Currency,
            invoice.IssuedAtUtc));
    }
}

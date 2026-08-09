#pragma warning disable CS1591
using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Payments.Models;
using LawConnect.Application.Features.Payments.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Payments.Commands;

/// <summary>Initiates appointment checkout.</summary>
public sealed record InitiateAppointmentPaymentCommand(Guid AppointmentId, string IdempotencyKey)
    : IRequest<Result<PaymentResponse>>;

public sealed class InitiateAppointmentPaymentCommandValidator : AbstractValidator<InitiateAppointmentPaymentCommand>
{
    public InitiateAppointmentPaymentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}

public sealed class InitiateAppointmentPaymentCommandHandler
    : IRequestHandler<InitiateAppointmentPaymentCommand, Result<PaymentResponse>>
{
    private readonly IPaymentService _payments;
    private readonly ICurrentCustomerService _customer;
    private readonly IApplicationDbContext _db;

    public InitiateAppointmentPaymentCommandHandler(
        IPaymentService payments,
        ICurrentCustomerService customer,
        IApplicationDbContext db)
    {
        _payments = payments;
        _customer = customer;
        _db = db;
    }

    public async Task<Result<PaymentResponse>> Handle(
        InitiateAppointmentPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _customer.GetOrCreateAsync(cancellationToken);
        var appointment = await _db.Appointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), request.AppointmentId);
        if (appointment.CustomerId != customer.Id)
        {
            throw new ForbiddenException("You are not allowed to pay for this appointment.");
        }

        return Result.Success(await _payments.InitiateAppointmentPaymentAsync(
            request.AppointmentId,
            request.IdempotencyKey,
            cancellationToken));
    }
}

/// <summary>Confirms appointment payment with provider signature.</summary>
public sealed record ConfirmAppointmentPaymentCommand(
    Guid PaymentId,
    string ProviderPaymentId,
    string Signature,
    string IdempotencyKey) : IRequest<Result<PaymentResponse>>;

public sealed class ConfirmAppointmentPaymentCommandValidator : AbstractValidator<ConfirmAppointmentPaymentCommand>
{
    public ConfirmAppointmentPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.ProviderPaymentId).NotEmpty();
        RuleFor(x => x.Signature).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}

public sealed class ConfirmAppointmentPaymentCommandHandler
    : IRequestHandler<ConfirmAppointmentPaymentCommand, Result<PaymentResponse>>
{
    private readonly IPaymentService _payments;
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _db;

    public ConfirmAppointmentPaymentCommandHandler(
        IPaymentService payments,
        ICurrentUser currentUser,
        IApplicationDbContext db)
    {
        _payments = payments;
        _currentUser = currentUser;
        _db = db;
    }

    public async Task<Result<PaymentResponse>> Handle(
        ConfirmAppointmentPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var payment = await _db.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);
        if (payment.UserId != userId)
        {
            throw new ForbiddenException("You are not allowed to confirm this payment.");
        }

        return Result.Success(await _payments.ConfirmAppointmentPaymentAsync(
            request.PaymentId,
            request.ProviderPaymentId,
            request.Signature,
            request.IdempotencyKey,
            cancellationToken));
    }
}

/// <summary>Refunds a payment.</summary>
public sealed record RefundPaymentCommand(Guid PaymentId, decimal? Amount, string? Reason, string IdempotencyKey)
    : IRequest<Result<RefundResponse>>;

public sealed class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}

public sealed class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, Result<RefundResponse>>
{
    private readonly IPaymentService _payments;

    public RefundPaymentCommandHandler(IPaymentService payments) => _payments = payments;

    public async Task<Result<RefundResponse>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken) =>
        Result.Success(await _payments.RefundPaymentAsync(
            request.PaymentId,
            request.Amount,
            request.Reason,
            request.IdempotencyKey,
            cancellationToken));
}

/// <summary>Tops up the current user's wallet.</summary>
public sealed record TopUpWalletCommand(decimal Amount, string IdempotencyKey) : IRequest<Result<WalletResponse>>;

public sealed class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}

public sealed class TopUpWalletCommandHandler : IRequestHandler<TopUpWalletCommand, Result<WalletResponse>>
{
    private readonly IPaymentService _payments;
    private readonly ICurrentUser _currentUser;

    public TopUpWalletCommandHandler(IPaymentService payments, ICurrentUser currentUser)
    {
        _payments = payments;
        _currentUser = currentUser;
    }

    public async Task<Result<WalletResponse>> Handle(TopUpWalletCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        await _payments.TopUpWalletAsync(userId, request.Amount, request.IdempotencyKey, cancellationToken);
        var wallet = await _payments.GetOrCreateWalletAsync(userId, cancellationToken);
        return Result.Success(new WalletResponse(wallet.Id, wallet.AvailableBalance, wallet.HeldBalance, wallet.Currency));
    }
}

/// <summary>Processes a provider webhook.</summary>
public sealed record ProcessPaymentWebhookCommand(
    string Provider,
    string EventId,
    string EventType,
    string RawBody,
    string Signature) : IRequest<Result>;

public sealed class ProcessPaymentWebhookCommandValidator : AbstractValidator<ProcessPaymentWebhookCommand>
{
    public ProcessPaymentWebhookCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty();
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.EventType).NotEmpty();
        RuleFor(x => x.Signature).NotEmpty();
    }
}

public sealed class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, Result>
{
    private readonly IPaymentService _payments;

    public ProcessPaymentWebhookCommandHandler(IPaymentService payments) => _payments = payments;

    public async Task<Result> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        await _payments.ProcessWebhookAsync(
            request.Provider,
            request.EventId,
            request.EventType,
            request.RawBody,
            request.Signature,
            cancellationToken);
        return Result.Success();
    }
}

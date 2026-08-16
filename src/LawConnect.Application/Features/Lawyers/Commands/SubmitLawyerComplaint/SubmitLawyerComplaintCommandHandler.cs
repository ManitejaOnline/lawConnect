using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Lawyers.Models;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LawConnect.Application.Features.Lawyers.Commands.SubmitLawyerComplaint;

/// <summary>
/// Handles <see cref="SubmitLawyerComplaintCommand"/>.
/// </summary>
public sealed class SubmitLawyerComplaintCommandHandler
    : IRequestHandler<SubmitLawyerComplaintCommand, Result<LawyerComplaintSubmittedResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly ILogger<SubmitLawyerComplaintCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitLawyerComplaintCommandHandler"/> class.
    /// </summary>
    public SubmitLawyerComplaintCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        ILogger<SubmitLawyerComplaintCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<LawyerComplaintSubmittedResponse>> Handle(
        SubmitLawyerComplaintCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);

        var lawyerExists = await _dbContext.Lawyers.AnyAsync(l => l.Id == request.LawyerId, cancellationToken);
        if (!lawyerExists)
        {
            throw new NotFoundException(nameof(Lawyer), request.LawyerId);
        }

        if (request.AppointmentId.HasValue)
        {
            var owns = await _dbContext.Appointments.AnyAsync(
                a => a.Id == request.AppointmentId.Value && a.CustomerId == customer.Id && a.LawyerId == request.LawyerId,
                cancellationToken);
            if (!owns)
            {
                throw new ForbiddenException("You can only reference your own appointment with this lawyer.");
            }
        }
        else
        {
            var hasEngagement = await _dbContext.Appointments.AnyAsync(
                a => a.CustomerId == customer.Id && a.LawyerId == request.LawyerId,
                cancellationToken);
            if (!hasEngagement)
            {
                throw new ForbiddenException("Only clients with an engagement with this lawyer can submit a complaint.");
            }
        }

        var complaint = new LawyerComplaint
        {
            LawyerId = request.LawyerId,
            ClientId = customer.Id,
            AppointmentId = request.AppointmentId,
            Category = request.Category.Trim(),
            Description = request.Description.Trim()
        };
        _dbContext.LawyerComplaints.Add(complaint);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Customer {CustomerId} submitted a complaint {ComplaintId} about lawyer {LawyerId}",
            customer.Id,
            complaint.Id,
            request.LawyerId);

        return Result.Success(new LawyerComplaintSubmittedResponse(
            complaint.Id,
            complaint.LawyerId,
            complaint.Category,
            complaint.Status.ToString(),
            complaint.CreatedAt));
    }
}

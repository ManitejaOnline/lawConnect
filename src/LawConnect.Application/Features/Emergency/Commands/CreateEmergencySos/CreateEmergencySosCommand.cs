using FluentValidation;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Models;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Emergency.Models;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using LawConnect.Domain.Exceptions;
using LawConnect.Shared.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Features.Emergency.Commands.CreateEmergencySos;

/// <summary>
/// Creates a customer emergency SOS request and starts matching.
/// </summary>
public sealed record CreateEmergencySosCommand(
    Guid CategoryId,
    double Latitude,
    double Longitude,
    string? Notes) : IRequest<Result<EmergencyRequestResponse>>;

/// <summary>
/// Validates <see cref="CreateEmergencySosCommand"/>.
/// </summary>
public sealed class CreateEmergencySosCommandValidator : AbstractValidator<CreateEmergencySosCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEmergencySosCommandValidator"/> class.
    /// </summary>
    public CreateEmergencySosCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

/// <summary>
/// Handles <see cref="CreateEmergencySosCommand"/>.
/// </summary>
public sealed class CreateEmergencySosCommandHandler
    : IRequestHandler<CreateEmergencySosCommand, Result<EmergencyRequestResponse>>
{
    private static readonly EmergencyStatus[] ActiveStatuses = EmergencyLifecycle.Open;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentCustomerService _currentCustomer;
    private readonly IEmergencyDispatchService _dispatch;
    private readonly ILocationResolver _locationResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateEmergencySosCommandHandler"/> class.
    /// </summary>
    public CreateEmergencySosCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentCustomerService currentCustomer,
        IEmergencyDispatchService dispatch,
        ILocationResolver locationResolver)
    {
        _dbContext = dbContext;
        _currentCustomer = currentCustomer;
        _dispatch = dispatch;
        _locationResolver = locationResolver;
    }

    /// <inheritdoc />
    public async Task<Result<EmergencyRequestResponse>> Handle(
        CreateEmergencySosCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await _currentCustomer.GetOrCreateAsync(cancellationToken);

        var hasActive = await _dbContext.EmergencyRequests
            .AnyAsync(
                r => r.CustomerId == customer.Id && ActiveStatuses.Contains(r.Status),
                cancellationToken);
        if (hasActive)
        {
            throw new ConflictException("You already have an active emergency SOS request.");
        }

        var category = await _dbContext.EmergencyCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.IsActive, cancellationToken)
            ?? throw new NotFoundException(nameof(EmergencyCategory), request.CategoryId);

        var place = await _locationResolver.ResolveFromCoordinatesAsync(
            request.Latitude,
            request.Longitude,
            cancellationToken);

        var sos = new EmergencyRequest
        {
            CustomerId = customer.Id,
            Customer = customer,
            CategoryId = category.Id,
            Category = category,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CustomerCountryId = place?.CountryId,
            CustomerStateId = place?.StateId,
            CustomerCityId = place?.CityId,
            CustomerCountryName = place?.CountryName,
            CustomerStateName = place?.StateName,
            CustomerCityName = place?.CityName,
            CurrentSearchScope = EmergencySearchScope.City,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Status = EmergencyStatus.Created,
            AttemptNumber = 1,
            CurrentRadiusKm = 0
        };

        _dbContext.EmergencyRequests.Add(sos);
        _dispatch.AddTimeline(
            sos,
            EmergencyTimelineEventTypes.Created,
            "Emergency SOS created.",
            customer.UserId,
            new { request.Latitude, request.Longitude, category.Name });
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(await _dispatch.DispatchAttemptAsync(sos, cancellationToken));
    }
}

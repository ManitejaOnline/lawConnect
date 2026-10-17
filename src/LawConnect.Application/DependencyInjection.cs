using System.Reflection;
using LawConnect.Application.Common.Behaviors;
using LawConnect.Application.Common.Geo;
using LawConnect.Application.Common.Interfaces;
using LawConnect.Application.Common.Realtime;
using LawConnect.Application.Common.Services;
using LawConnect.Application.Features.Appointments.Services;
using LawConnect.Application.Features.Auth.Services;
using LawConnect.Application.Features.Cases.Services;
using LawConnect.Application.Features.Chat.Services;
using LawConnect.Application.Features.Documents;
using LawConnect.Application.Features.Emergency.Services;
using LawConnect.Application.Features.Lawyers.Services;
using LawConnect.Application.Features.Payments.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LawConnect.Application;

/// <summary>
/// Application-layer dependency injection registration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR, FluentValidation, AutoMapper, pipeline behaviors, and application services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddAutoMapper(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<IAuthTokenIssuer, AuthTokenIssuer>();
        services.AddScoped<ICurrentLawyerService, CurrentLawyerService>();
        services.AddScoped<ICurrentCustomerService, CurrentCustomerService>();
        services.AddScoped<IAppointmentSlotValidator, AppointmentSlotValidator>();
        services.AddScoped<IConversationAccessService, ConversationAccessService>();
        services.AddScoped<ILocationResolver, LocationResolver>();
        services.AddScoped<IEmergencyLawyerMatcher, EmergencyLawyerMatcher>();
        services.AddScoped<IEmergencyDispatchService, EmergencyDispatchService>();
        services.AddScoped<IEmergencyTimeoutProcessor, EmergencyTimeoutProcessor>();
        services.AddSingleton<IEmergencyAcceptCoordinator, EmergencyAcceptCoordinator>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddSingleton<IPresenceTracker, PresenceTracker>();
        services.AddSingleton<IDocumentTransferRelay, DocumentTransferRelay>();
        services.AddScoped<IDocumentAccess, DocumentAccess>();
        services.AddSingleton<ILawyerProfileCache, LawyerProfileCache>();
        services.AddSingleton<ILawyerSearchCache, LawyerSearchCache>();
        services.AddScoped<ITrustScoreService, TrustScoreService>();
        services.AddScoped<IOverdueMilestoneSweepService, OverdueMilestoneSweepService>();

        return services;
    }
}

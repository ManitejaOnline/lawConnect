using LawConnect.Application.Common.Interfaces;
using LawConnect.Domain.Common;
using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for LawConnect.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </summary>
    /// <param name="options">DbContext options.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<User> Users => Set<User>();

    /// <inheritdoc />
    public DbSet<Role> Roles => Set<Role>();

    /// <inheritdoc />
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <inheritdoc />
    public DbSet<Customer> Customers => Set<Customer>();

    /// <inheritdoc />
    public DbSet<Lawyer> Lawyers => Set<Lawyer>();

    /// <inheritdoc />
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();

    /// <inheritdoc />
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    /// <inheritdoc />
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <inheritdoc />
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    /// <inheritdoc />
    public DbSet<Device> Devices => Set<Device>();

    /// <inheritdoc />
    public DbSet<Address> Addresses => Set<Address>();

    /// <inheritdoc />
    public DbSet<Country> Countries => Set<Country>();

    /// <inheritdoc />
    public DbSet<State> States => Set<State>();

    /// <inheritdoc />
    public DbSet<City> Cities => Set<City>();

    /// <inheritdoc />
    public DbSet<Specialization> Specializations => Set<Specialization>();

    /// <inheritdoc />
    public DbSet<LawyerSpecialization> LawyerSpecializations => Set<LawyerSpecialization>();

    /// <inheritdoc />
    public DbSet<LawyerAvailability> LawyerAvailabilities => Set<LawyerAvailability>();

    /// <inheritdoc />
    public DbSet<LawyerEducation> LawyerEducations => Set<LawyerEducation>();

    /// <inheritdoc />
    public DbSet<LawyerExperience> LawyerExperiences => Set<LawyerExperience>();

    /// <inheritdoc />
    public DbSet<LawyerLanguage> LawyerLanguages => Set<LawyerLanguage>();

    /// <inheritdoc />
    public DbSet<LawyerDocument> LawyerDocuments => Set<LawyerDocument>();

    /// <inheritdoc />
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <inheritdoc />
    public DbSet<Conversation> Conversations => Set<Conversation>();

    /// <inheritdoc />
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();

    /// <inheritdoc />
    public DbSet<Message> Messages => Set<Message>();

    /// <inheritdoc />
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    /// <inheritdoc />
    public DbSet<MessageReadStatus> MessageReadStatuses => Set<MessageReadStatus>();

    /// <inheritdoc />
    public DbSet<EmergencyCategory> EmergencyCategories => Set<EmergencyCategory>();

    /// <inheritdoc />
    public DbSet<EmergencyRequest> EmergencyRequests => Set<EmergencyRequest>();

    /// <inheritdoc />
    public DbSet<EmergencyAssignment> EmergencyAssignments => Set<EmergencyAssignment>();

    /// <inheritdoc />
    public DbSet<EmergencyTimelineEvent> EmergencyTimelineEvents => Set<EmergencyTimelineEvent>();

    /// <inheritdoc />
    public DbSet<EmergencyHistory> EmergencyHistories => Set<EmergencyHistory>();

    /// <inheritdoc />
    public DbSet<Wallet> Wallets => Set<Wallet>();

    /// <inheritdoc />
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    /// <inheritdoc />
    public DbSet<Payment> Payments => Set<Payment>();

    /// <inheritdoc />
    public DbSet<Refund> Refunds => Set<Refund>();

    /// <inheritdoc />
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <inheritdoc />
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();

    /// <inheritdoc />
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var method = typeof(ApplicationDbContext)
                .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [modelBuilder]);
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
}

using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LawConnect.Application.Common.Interfaces;

/// <summary>
/// Application persistence abstraction over the LawConnect database.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Gets users.</summary>
    DbSet<User> Users { get; }

    /// <summary>Gets roles.</summary>
    DbSet<Role> Roles { get; }

    /// <summary>Gets user-role assignments.</summary>
    DbSet<UserRole> UserRoles { get; }

    /// <summary>Gets customer profiles.</summary>
    DbSet<Customer> Customers { get; }

    /// <summary>Gets lawyer profiles.</summary>
    DbSet<Lawyer> Lawyers { get; }

    /// <summary>Gets refresh tokens.</summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>Gets OTP verification records.</summary>
    DbSet<OtpVerification> OtpVerifications { get; }

    /// <summary>Gets audit log entries.</summary>
    DbSet<AuditLog> AuditLogs { get; }

    /// <summary>Gets notifications.</summary>
    DbSet<Notification> Notifications { get; }

    /// <summary>Gets notification channel preferences.</summary>
    DbSet<NotificationPreference> NotificationPreferences { get; }

    /// <summary>Gets registered devices.</summary>
    DbSet<Device> Devices { get; }

    /// <summary>Gets addresses.</summary>
    DbSet<Address> Addresses { get; }

    /// <summary>Gets countries.</summary>
    DbSet<Country> Countries { get; }

    /// <summary>Gets states.</summary>
    DbSet<State> States { get; }

    /// <summary>Gets cities.</summary>
    DbSet<City> Cities { get; }

    /// <summary>Gets legal specializations.</summary>
    DbSet<Specialization> Specializations { get; }

    /// <summary>Gets lawyer-specialization links.</summary>
    DbSet<LawyerSpecialization> LawyerSpecializations { get; }

    /// <summary>Gets lawyer availability slots.</summary>
    DbSet<LawyerAvailability> LawyerAvailabilities { get; }

    /// <summary>Gets lawyer education records.</summary>
    DbSet<LawyerEducation> LawyerEducations { get; }

    /// <summary>Gets lawyer experience records.</summary>
    DbSet<LawyerExperience> LawyerExperiences { get; }

    /// <summary>Gets lawyer languages.</summary>
    DbSet<LawyerLanguage> LawyerLanguages { get; }

    /// <summary>Gets lawyer documents.</summary>
    DbSet<LawyerDocument> LawyerDocuments { get; }

    /// <summary>Gets appointments.</summary>
    DbSet<Appointment> Appointments { get; }

    /// <summary>Gets conversations.</summary>
    DbSet<Conversation> Conversations { get; }

    /// <summary>Gets conversation participants.</summary>
    DbSet<ConversationParticipant> ConversationParticipants { get; }

    /// <summary>Gets chat messages.</summary>
    DbSet<Message> Messages { get; }

    /// <summary>Gets message attachments.</summary>
    DbSet<MessageAttachment> MessageAttachments { get; }

    /// <summary>Gets message read/delivery statuses.</summary>
    DbSet<MessageReadStatus> MessageReadStatuses { get; }

    /// <summary>Gets emergency categories.</summary>
    DbSet<EmergencyCategory> EmergencyCategories { get; }

    /// <summary>Gets emergency SOS requests.</summary>
    DbSet<EmergencyRequest> EmergencyRequests { get; }

    /// <summary>Gets emergency lawyer assignments.</summary>
    DbSet<EmergencyAssignment> EmergencyAssignments { get; }

    /// <summary>Gets emergency timeline events.</summary>
    DbSet<EmergencyTimelineEvent> EmergencyTimelineEvents { get; }

    /// <summary>Gets emergency history snapshots.</summary>
    DbSet<EmergencyHistory> EmergencyHistories { get; }

    /// <summary>Gets wallets.</summary>
    DbSet<Wallet> Wallets { get; }

    /// <summary>Gets wallet ledger transactions.</summary>
    DbSet<WalletTransaction> WalletTransactions { get; }

    /// <summary>Gets payments.</summary>
    DbSet<Payment> Payments { get; }

    /// <summary>Gets refunds.</summary>
    DbSet<Refund> Refunds { get; }

    /// <summary>Gets invoices.</summary>
    DbSet<Invoice> Invoices { get; }

    /// <summary>Gets saved payment methods.</summary>
    DbSet<PaymentMethod> PaymentMethods { get; }

    /// <summary>Gets outbox messages.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>Gets processed webhook receipts.</summary>
    DbSet<ProcessedWebhook> ProcessedWebhooks { get; }

    /// <summary>Gets idempotency records.</summary>
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    /// <summary>
    /// Persists pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

#pragma warning disable CS1591
using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration : BaseEntityConfiguration<Wallet>
{
    public override void Configure(EntityTypeBuilder<Wallet> builder)
    {
        base.Configure(builder);
        builder.ToTable("Wallets");
        builder.Property(x => x.AvailableBalance).HasPrecision(18, 2);
        builder.Property(x => x.HeldBalance).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WalletTransactionConfiguration : BaseEntityConfiguration<WalletTransaction>
{
    public override void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        base.Configure(builder);
        builder.ToTable("WalletTransactions");
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.AvailableBalanceAfter).HasPrecision(18, 2);
        builder.Property(x => x.HeldBalanceAfter).HasPrecision(18, 2);
        builder.Property(x => x.ReferenceType).HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(x => new { x.WalletId, x.CreatedAt });
        builder.HasOne(x => x.Wallet).WithMany(x => x.Transactions).HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PaymentConfiguration : BaseEntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);
        builder.ToTable("Payments");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ProviderOrderId).HasMaxLength(128);
        builder.Property(x => x.ProviderPaymentId).HasMaxLength(128);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FailureReason).HasMaxLength(512);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.ProviderOrderId);
        builder.HasIndex(x => x.ProviderPaymentId);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Appointment)
            .WithOne(x => x.Payment)
            .HasForeignKey<Payment>(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.EmergencyRequest)
            .WithOne(x => x.Payment)
            .HasForeignKey<Payment>(x => x.EmergencyRequestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RefundConfiguration : BaseEntityConfiguration<Refund>
{
    public override void Configure(EntityTypeBuilder<Refund> builder)
    {
        base.Configure(builder);
        builder.ToTable("Refunds");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ProviderRefundId).HasMaxLength(128);
        builder.Property(x => x.Reason).HasMaxLength(512);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasOne(x => x.Payment).WithMany(x => x.Refunds).HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InvoiceConfiguration : BaseEntityConfiguration<Invoice>
{
    public override void Configure(EntityTypeBuilder<Invoice> builder)
    {
        base.Configure(builder);
        builder.ToTable("Invoices");
        builder.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.LineItemsJson).HasMaxLength(4000);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.PaymentId).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Payment).WithOne(x => x.Invoice).HasForeignKey<Invoice>(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PaymentMethodConfiguration : BaseEntityConfiguration<PaymentMethod>
{
    public override void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        base.Configure(builder);
        builder.ToTable("PaymentMethods");
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ProviderMethodToken).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(64);
        builder.Property(x => x.Last4).HasMaxLength(4);
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OutboxMessageConfiguration : BaseEntityConfiguration<OutboxMessage>
{
    public override void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        base.Configure(builder);
        builder.ToTable("OutboxMessages");
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadJson).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
    }
}

public sealed class ProcessedWebhookConfiguration : BaseEntityConfiguration<ProcessedWebhook>
{
    public override void Configure(EntityTypeBuilder<ProcessedWebhook> builder)
    {
        base.Configure(builder);
        builder.ToTable("ProcessedWebhooks");
        builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
        builder.Property(x => x.EventId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.Provider, x.EventId }).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}

public sealed class IdempotencyRecordConfiguration : BaseEntityConfiguration<IdempotencyRecord>
{
    public override void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        base.Configure(builder);
        builder.ToTable("IdempotencyRecords");
        builder.Property(x => x.Key).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResponseJson).HasMaxLength(8000).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}

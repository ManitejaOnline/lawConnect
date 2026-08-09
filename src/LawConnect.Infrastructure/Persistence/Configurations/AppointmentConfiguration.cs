using LawConnect.Domain.Entities;
using LawConnect.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : BaseEntityConfiguration<Appointment>
{
    public override void Configure(EntityTypeBuilder<Appointment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Appointments");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.Property(x => x.ConsultationFeeSnapshot).HasPrecision(18, 2);
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => new { x.LawyerId, x.StartUtc, x.EndUtc });
        builder.HasIndex(x => new { x.CustomerId, x.StartUtc });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.LawyerId, x.Status, x.StartUtc });

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Lawyer)
            .WithMany()
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

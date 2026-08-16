using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CaseConfiguration : BaseEntityConfiguration<Case>
{
    public override void Configure(EntityTypeBuilder<Case> builder)
    {
        base.Configure(builder);

        builder.ToTable("Cases");

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CaseNumber).HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.CourtName).HasMaxLength(256);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => x.LawyerId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Lawyer)
            .WithMany()
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Specialization)
            .WithMany()
            .HasForeignKey(x => x.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SourceAppointment)
            .WithMany()
            .HasForeignKey(x => x.SourceAppointmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

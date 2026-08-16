using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerComplaintConfiguration : BaseEntityConfiguration<LawyerComplaint>
{
    public override void Configure(EntityTypeBuilder<LawyerComplaint> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerComplaints");

        builder.Property(x => x.Category).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.AdminNotes).HasMaxLength(2000);

        builder.HasIndex(x => x.LawyerId);
        builder.HasIndex(x => x.ClientId);
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Lawyer)
            .WithMany()
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

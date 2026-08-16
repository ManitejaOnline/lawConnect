using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerRatingConfiguration : BaseEntityConfiguration<LawyerRating>
{
    public override void Configure(EntityTypeBuilder<LawyerRating> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerRatings");

        builder.Property(x => x.Stars).IsRequired();
        builder.Property(x => x.ReviewText).HasMaxLength(2000);

        builder.HasIndex(x => x.LawyerId);
        builder.HasIndex(x => x.ClientId);
        builder.HasIndex(x => x.AppointmentId).IsUnique();
        builder.HasIndex(x => x.CaseId).IsUnique();

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
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

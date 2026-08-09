using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerConfiguration : BaseEntityConfiguration<Lawyer>
{
    public override void Configure(EntityTypeBuilder<Lawyer> builder)
    {
        base.Configure(builder);

        builder.ToTable("Lawyers");

        builder.Property(x => x.BarCouncilNumber).HasMaxLength(64);
        builder.Property(x => x.StateBarCouncil).HasMaxLength(128);
        builder.Property(x => x.Bio).HasMaxLength(4000);
        builder.Property(x => x.CityName).HasMaxLength(128);
        builder.Property(x => x.StateName).HasMaxLength(128);
        builder.Property(x => x.CountryName).HasMaxLength(128);
        builder.Property(x => x.ReviewReason).HasMaxLength(2000);
        builder.HasIndex(x => x.ApplicationSubmittedAtUtc);
        builder.Property(x => x.ConsultationFee).HasPrecision(18, 2);
        builder.Property(x => x.EmergencyFee).HasPrecision(18, 2);
        builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => x.BarCouncilNumber)
            .IsUnique()
            .HasFilter("\"BarCouncilNumber\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.IsVerified);
        builder.HasIndex(x => x.IsAvailableForEmergency);
        builder.HasIndex(x => x.IsOnline);
        builder.HasIndex(x => x.VerificationStatus);
        builder.HasIndex(x => x.RatingAverage);
    }
}

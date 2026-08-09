using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerAvailabilityConfiguration : BaseEntityConfiguration<LawyerAvailability>
{
    public override void Configure(EntityTypeBuilder<LawyerAvailability> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerAvailabilities");

        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => new { x.LawyerId, x.DayOfWeek, x.StartTime, x.EndTime })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(x => x.Lawyer)
            .WithMany(x => x.Availabilities)
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

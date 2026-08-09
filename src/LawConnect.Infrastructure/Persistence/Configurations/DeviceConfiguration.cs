using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : BaseEntityConfiguration<Device>
{
    public override void Configure(EntityTypeBuilder<Device> builder)
    {
        base.Configure(builder);

        builder.ToTable("Devices");

        builder.Property(x => x.DeviceIdentifier).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FcmToken).HasMaxLength(512);
        builder.Property(x => x.Platform).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.AppVersion).HasMaxLength(32);
        builder.Property(x => x.OsVersion).HasMaxLength(64);

        builder.HasIndex(x => new { x.UserId, x.DeviceIdentifier })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.FcmToken)
            .HasFilter("\"FcmToken\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasOne(x => x.User)
            .WithMany(x => x.Devices)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

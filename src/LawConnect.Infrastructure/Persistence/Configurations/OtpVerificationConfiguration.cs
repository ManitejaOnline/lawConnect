using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class OtpVerificationConfiguration : BaseEntityConfiguration<OtpVerification>
{
    public override void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        base.Configure(builder);

        builder.ToTable("OtpVerifications");

        builder.Property(x => x.Destination).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Purpose).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => new { x.Destination, x.Purpose, x.IsUsed });
        builder.HasIndex(x => x.ExpiresAtUtc);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

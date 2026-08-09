using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : BaseEntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);

        builder.ToTable("RefreshTokens");

        builder.Property(x => x.Token).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ReplacedByToken).HasMaxLength(512);
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);

        builder.Ignore(x => x.IsExpired);
        builder.Ignore(x => x.IsRevoked);
        builder.Ignore(x => x.IsActive);

        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAtUtc);

        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

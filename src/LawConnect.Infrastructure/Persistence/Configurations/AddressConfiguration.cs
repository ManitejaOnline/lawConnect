using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class AddressConfiguration : BaseEntityConfiguration<Address>
{
    public override void Configure(EntityTypeBuilder<Address> builder)
    {
        base.Configure(builder);

        builder.ToTable("Addresses");

        builder.Property(x => x.Line1).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Line2).HasMaxLength(256);
        builder.Property(x => x.PostalCode).HasMaxLength(16);
        builder.Property(x => x.Landmark).HasMaxLength(256);
        builder.Property(x => x.Label).HasMaxLength(64);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsPrimary });
        builder.HasIndex(x => x.CityId);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.City)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.CityId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

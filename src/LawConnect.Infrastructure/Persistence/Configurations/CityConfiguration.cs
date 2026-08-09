using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : BaseEntityConfiguration<City>
{
    public override void Configure(EntityTypeBuilder<City> builder)
    {
        base.Configure(builder);

        builder.ToTable("Cities");

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.StateId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(x => x.State)
            .WithMany(x => x.Cities)
            .HasForeignKey(x => x.StateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

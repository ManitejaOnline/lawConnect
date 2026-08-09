using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class StateConfiguration : BaseEntityConfiguration<State>
{
    public override void Configure(EntityTypeBuilder<State> builder)
    {
        base.Configure(builder);

        builder.ToTable("States");

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(16);

        builder.HasIndex(x => new { x.CountryId, x.Name })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(x => x.Country)
            .WithMany(x => x.States)
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

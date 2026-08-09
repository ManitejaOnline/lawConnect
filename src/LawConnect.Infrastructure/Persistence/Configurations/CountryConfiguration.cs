using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CountryConfiguration : BaseEntityConfiguration<Country>
{
    public override void Configure(EntityTypeBuilder<Country> builder)
    {
        base.Configure(builder);

        builder.ToTable("Countries");

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Iso2Code).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Iso3Code).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PhoneCode).HasMaxLength(8);

        builder.HasIndex(x => x.Iso2Code).IsUnique();
        builder.HasIndex(x => x.Iso3Code).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

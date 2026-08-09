using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : BaseEntityConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        base.Configure(builder);

        builder.ToTable("Customers");

        builder.Property(x => x.Gender).HasMaxLength(32);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(16);

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

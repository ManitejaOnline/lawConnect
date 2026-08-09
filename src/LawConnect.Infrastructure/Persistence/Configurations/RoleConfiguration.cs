using LawConnect.Domain.Entities;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : BaseEntityConfiguration<Role>
{
    public override void Configure(EntityTypeBuilder<Role> builder)
    {
        base.Configure(builder);

        builder.ToTable("Roles");

        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(256);

        builder.HasIndex(x => x.Name).IsUnique();

        var seedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new Role
            {
                Id = SeedIds.Roles.Customer,
                Name = Roles.Customer,
                Description = "End customer seeking legal services",
                CreatedAt = seedAt,
                IsDeleted = false,
                RowVersion = 0
            },
            new Role
            {
                Id = SeedIds.Roles.Lawyer,
                Name = Roles.Lawyer,
                Description = "Verified lawyer providing legal services",
                CreatedAt = seedAt,
                IsDeleted = false,
                RowVersion = 0
            },
            new Role
            {
                Id = SeedIds.Roles.Admin,
                Name = Roles.Admin,
                Description = "Platform administrator",
                CreatedAt = seedAt,
                IsDeleted = false,
                RowVersion = 0
            });
    }
}

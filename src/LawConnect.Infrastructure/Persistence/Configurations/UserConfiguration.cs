using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : BaseEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("Users");

        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.PasswordHash).HasMaxLength(512);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProfileImageUrl).HasMaxLength(1024);

        builder.HasIndex(x => x.PhoneNumber).IsUnique();
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.Customer)
            .WithOne(x => x.User)
            .HasForeignKey<Customer>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Lawyer)
            .WithOne(x => x.User)
            .HasForeignKey<Lawyer>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.NotificationPreference)
            .WithOne(x => x.User)
            .HasForeignKey<NotificationPreference>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

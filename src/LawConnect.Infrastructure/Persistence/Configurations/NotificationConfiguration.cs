using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : BaseEntityConfiguration<Notification>
{
    public override void Configure(EntityTypeBuilder<Notification> builder)
    {
        base.Configure(builder);

        builder.ToTable("Notifications");

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Channel).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsRead });
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

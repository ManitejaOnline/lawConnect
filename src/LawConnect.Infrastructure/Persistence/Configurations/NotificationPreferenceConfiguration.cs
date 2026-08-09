using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : BaseEntityConfiguration<NotificationPreference>
{
    public override void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        base.Configure(builder);

        builder.ToTable("NotificationPreferences");

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}

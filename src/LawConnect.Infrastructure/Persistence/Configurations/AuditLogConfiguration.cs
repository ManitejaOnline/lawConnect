using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    public override void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("AuditLogs");

        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.CorrelationId).HasMaxLength(64);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.EntityName);
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.User)
            .WithMany(x => x.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

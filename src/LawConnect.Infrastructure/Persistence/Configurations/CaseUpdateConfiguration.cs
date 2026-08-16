using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CaseUpdateConfiguration : BaseEntityConfiguration<CaseUpdate>
{
    public override void Configure(EntityTypeBuilder<CaseUpdate> builder)
    {
        base.Configure(builder);

        builder.ToTable("CaseUpdates");

        builder.Property(x => x.UpdateType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.CaseId);

        builder.HasOne(x => x.Case)
            .WithMany(x => x.CaseUpdates)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CaseMilestoneConfiguration : BaseEntityConfiguration<CaseMilestone>
{
    public override void Configure(EntityTypeBuilder<CaseMilestone> builder)
    {
        base.Configure(builder);

        builder.ToTable("CaseMilestones");

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        builder.HasIndex(x => x.CaseId);
        builder.HasIndex(x => x.DueDate);

        builder.HasOne(x => x.Case)
            .WithMany(x => x.Milestones)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

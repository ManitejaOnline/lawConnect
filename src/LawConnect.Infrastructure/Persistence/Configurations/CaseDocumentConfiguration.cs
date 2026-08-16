using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CaseDocumentConfiguration : BaseEntityConfiguration<CaseDocument>
{
    public override void Configure(EntityTypeBuilder<CaseDocument> builder)
    {
        base.Configure(builder);

        builder.ToTable("CaseDocuments");

        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.CaseId);
        builder.HasIndex(x => x.StorageKey).IsUnique();

        builder.HasOne(x => x.Case)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CaseUpdate)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.CaseUpdateId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Hearing)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.HearingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Milestone)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.MilestoneId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

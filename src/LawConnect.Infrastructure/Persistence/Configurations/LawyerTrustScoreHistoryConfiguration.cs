using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerTrustScoreHistoryConfiguration : BaseEntityConfiguration<LawyerTrustScoreHistory>
{
    public override void Configure(EntityTypeBuilder<LawyerTrustScoreHistory> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerTrustScoreHistories");

        builder.Property(x => x.Level).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CaseManagementNote).HasMaxLength(500);

        builder.HasIndex(x => x.LawyerId);
        builder.HasIndex(x => x.ComputedAtUtc);

        builder.HasOne(x => x.Lawyer)
            .WithMany()
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

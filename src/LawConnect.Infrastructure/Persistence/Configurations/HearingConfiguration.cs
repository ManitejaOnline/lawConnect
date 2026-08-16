using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class HearingConfiguration : BaseEntityConfiguration<Hearing>
{
    public override void Configure(EntityTypeBuilder<Hearing> builder)
    {
        base.Configure(builder);

        builder.ToTable("Hearings");

        builder.Property(x => x.CourtName).HasMaxLength(256);
        builder.Property(x => x.HearingType).HasMaxLength(128);
        builder.Property(x => x.Purpose).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Outcome).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.NextAction).HasMaxLength(500);

        builder.HasIndex(x => x.CaseId);
        builder.HasIndex(x => x.HearingDate);

        builder.HasOne(x => x.Case)
            .WithMany(x => x.Hearings)
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

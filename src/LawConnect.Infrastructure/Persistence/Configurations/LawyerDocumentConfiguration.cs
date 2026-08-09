using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerDocumentConfiguration : BaseEntityConfiguration<LawyerDocument>
{
    public override void Configure(EntityTypeBuilder<LawyerDocument> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerDocuments");

        builder.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.VerificationStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.HasIndex(x => x.LawyerId);
        builder.HasIndex(x => x.StorageKey).IsUnique();
        builder.HasIndex(x => x.VerificationStatus);

        builder.HasOne(x => x.Lawyer)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

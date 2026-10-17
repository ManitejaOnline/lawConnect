using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class DocumentTransferSessionConfiguration : BaseEntityConfiguration<DocumentTransferSession>
{
    public override void Configure(EntityTypeBuilder<DocumentTransferSession> builder)
    {
        base.Configure(builder);
        builder.ToTable("DocumentTransferSessions");
        builder.Property(x => x.UploadTokenHash).HasMaxLength(128);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.DocumentId, x.Status });
        builder.HasIndex(x => x.DocumentId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Pending', 'Approved', 'Transferring') AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.LawyerUserId);

        builder.HasOne(x => x.Document)
            .WithMany(x => x.Transfers)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LawyerUser)
            .WithMany()
            .HasForeignKey(x => x.LawyerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

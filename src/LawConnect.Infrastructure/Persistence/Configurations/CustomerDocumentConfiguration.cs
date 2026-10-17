using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class CustomerDocumentConfiguration : BaseEntityConfiguration<CustomerDocument>
{
    public override void Configure(EntityTypeBuilder<CustomerDocument> builder)
    {
        base.Configure(builder);
        builder.ToTable("CustomerDocuments");
        builder.Property(x => x.FileName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.CaseId);
        builder.HasIndex(x => x.AppointmentId);

        builder.HasOne(x => x.Owner)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Case)
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

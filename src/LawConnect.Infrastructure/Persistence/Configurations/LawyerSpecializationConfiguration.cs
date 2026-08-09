using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerSpecializationConfiguration : BaseEntityConfiguration<LawyerSpecialization>
{
    public override void Configure(EntityTypeBuilder<LawyerSpecialization> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerSpecializations");

        builder.HasIndex(x => new { x.LawyerId, x.SpecializationId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.SpecializationId);

        builder.HasOne(x => x.Lawyer)
            .WithMany(x => x.LawyerSpecializations)
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Specialization)
            .WithMany(x => x.LawyerSpecializations)
            .HasForeignKey(x => x.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

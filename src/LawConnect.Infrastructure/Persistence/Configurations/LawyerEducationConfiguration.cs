using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerEducationConfiguration : BaseEntityConfiguration<LawyerEducation>
{
    public override void Configure(EntityTypeBuilder<LawyerEducation> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerEducations");

        builder.Property(x => x.InstitutionName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Degree).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FieldOfStudy).HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(1000);

        builder.HasIndex(x => x.LawyerId);

        builder.HasOne(x => x.Lawyer)
            .WithMany(x => x.Educations)
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

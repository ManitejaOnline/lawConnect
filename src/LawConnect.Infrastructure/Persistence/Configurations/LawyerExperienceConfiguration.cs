using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerExperienceConfiguration : BaseEntityConfiguration<LawyerExperience>
{
    public override void Configure(EntityTypeBuilder<LawyerExperience> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerExperiences");

        builder.Property(x => x.OrganizationName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.HasIndex(x => x.LawyerId);

        builder.HasOne(x => x.Lawyer)
            .WithMany(x => x.Experiences)
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

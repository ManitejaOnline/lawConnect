using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class LawyerLanguageConfiguration : BaseEntityConfiguration<LawyerLanguage>
{
    public override void Configure(EntityTypeBuilder<LawyerLanguage> builder)
    {
        base.Configure(builder);

        builder.ToTable("LawyerLanguages");

        builder.Property(x => x.LanguageCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.LanguageName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Proficiency).HasMaxLength(32);

        builder.HasIndex(x => new { x.LawyerId, x.LanguageCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(x => x.Lawyer)
            .WithMany(x => x.Languages)
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

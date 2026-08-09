using LawConnect.Domain.Entities;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class SpecializationConfiguration : BaseEntityConfiguration<Specialization>
{
    public override void Configure(EntityTypeBuilder<Specialization> builder)
    {
        base.Configure(builder);

        builder.ToTable("Specializations");

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Slug).HasMaxLength(128);

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL AND \"IsDeleted\" = false");
        builder.HasIndex(x => x.DisplayOrder);

        var seedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            Create(SeedIds.Specializations.CriminalLaw, "Criminal Law", "criminal-law", 1, seedAt),
            Create(SeedIds.Specializations.CivilLaw, "Civil Law", "civil-law", 2, seedAt),
            Create(SeedIds.Specializations.FamilyLaw, "Family Law", "family-law", 3, seedAt),
            Create(SeedIds.Specializations.CorporateLaw, "Corporate Law", "corporate-law", 4, seedAt),
            Create(SeedIds.Specializations.PropertyLaw, "Property Law", "property-law", 5, seedAt),
            Create(SeedIds.Specializations.IntellectualProperty, "Intellectual Property", "intellectual-property", 6, seedAt),
            Create(SeedIds.Specializations.TaxLaw, "Tax Law", "tax-law", 7, seedAt),
            Create(SeedIds.Specializations.LaborEmployment, "Labor & Employment", "labor-employment", 8, seedAt),
            Create(SeedIds.Specializations.ConstitutionalLaw, "Constitutional Law", "constitutional-law", 9, seedAt),
            Create(SeedIds.Specializations.ConsumerProtection, "Consumer Protection", "consumer-protection", 10, seedAt),
            Create(SeedIds.Specializations.CyberLaw, "Cyber Law", "cyber-law", 11, seedAt),
            Create(SeedIds.Specializations.ImmigrationLaw, "Immigration Law", "immigration-law", 12, seedAt),
            Create(SeedIds.Specializations.EnvironmentalLaw, "Environmental Law", "environmental-law", 13, seedAt),
            Create(SeedIds.Specializations.BankingFinance, "Banking & Finance", "banking-finance", 14, seedAt),
            Create(SeedIds.Specializations.MedicalNegligence, "Medical Negligence", "medical-negligence", 15, seedAt));
    }

    private static Specialization Create(Guid id, string name, string slug, int order, DateTime seedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = $"{name} specialization",
            DisplayOrder = order,
            IsActive = true,
            CreatedAt = seedAt,
            IsDeleted = false,
            RowVersion = 0
        };
}

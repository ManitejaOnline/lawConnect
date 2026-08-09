using LawConnect.Domain.Entities;
using LawConnect.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class EmergencyCategoryConfiguration : BaseEntityConfiguration<EmergencyCategory>
{
    public override void Configure(EntityTypeBuilder<EmergencyCategory> builder)
    {
        base.Configure(builder);
        builder.ToTable("EmergencyCategories");
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.HasIndex(x => x.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasOne(x => x.Specialization)
            .WithMany()
            .HasForeignKey(x => x.SpecializationId)
            .OnDelete(DeleteBehavior.Restrict);

        var seedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            Create(
                SeedIds.EmergencyCategories.CriminalEmergency,
                "Criminal Emergency",
                "criminal-emergency",
                "Immediate criminal legal assistance.",
                SeedIds.Specializations.CriminalLaw,
                1,
                seedAt),
            Create(
                SeedIds.EmergencyCategories.CivilEmergency,
                "Civil Emergency",
                "civil-emergency",
                "Urgent civil dispute assistance.",
                SeedIds.Specializations.CivilLaw,
                2,
                seedAt),
            Create(
                SeedIds.EmergencyCategories.FamilyCrisis,
                "Family Crisis",
                "family-crisis",
                "Family law emergency support.",
                SeedIds.Specializations.FamilyLaw,
                3,
                seedAt),
            Create(
                SeedIds.EmergencyCategories.CyberCrime,
                "Cyber Crime",
                "cyber-crime",
                "Cybercrime and digital fraud emergency.",
                SeedIds.Specializations.CyberLaw,
                4,
                seedAt),
            Create(
                SeedIds.EmergencyCategories.ConsumerEmergency,
                "Consumer Emergency",
                "consumer-emergency",
                "Consumer protection emergency.",
                SeedIds.Specializations.ConsumerProtection,
                5,
                seedAt));
    }

    private static EmergencyCategory Create(
        Guid id,
        string name,
        string slug,
        string description,
        Guid specializationId,
        int order,
        DateTime seedAt) =>
        new()
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = description,
            SpecializationId = specializationId,
            IsActive = true,
            DisplayOrder = order,
            CreatedAt = seedAt,
            IsDeleted = false,
            RowVersion = 0
        };
}

public sealed class EmergencyRequestConfiguration : BaseEntityConfiguration<EmergencyRequest>
{
    public override void Configure(EntityTypeBuilder<EmergencyRequest> builder)
    {
        base.Configure(builder);
        builder.ToTable("EmergencyRequests");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CurrentSearchScope).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CancellationReason).HasMaxLength(512);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CustomerCityName).HasMaxLength(128);
        builder.Property(x => x.CustomerStateName).HasMaxLength(128);
        builder.Property(x => x.CustomerCountryName).HasMaxLength(128);
        builder.Property(x => x.EmergencyFeeSnapshot).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.Status, x.SearchExpiresAtUtc });
        builder.HasIndex(x => new { x.CustomerCountryId, x.CustomerStateId, x.CustomerCityId });

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedLawyer)
            .WithMany()
            .HasForeignKey(x => x.AssignedLawyerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmergencyAssignmentConfiguration : BaseEntityConfiguration<EmergencyAssignment>
{
    public override void Configure(EntityTypeBuilder<EmergencyAssignment> builder)
    {
        base.Configure(builder);
        builder.ToTable("EmergencyAssignments");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CancellationReason).HasMaxLength(512);
        builder.HasIndex(x => new { x.EmergencyRequestId, x.LawyerId, x.AttemptNumber })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => new { x.LawyerId, x.Status });
        builder.HasIndex(x => new { x.EmergencyRequestId, x.AttemptNumber, x.Status });
        builder.HasIndex(x => x.EmergencyRequestId)
            .IsUnique()
            .HasDatabaseName("UX_EmergencyAssignments_OneAccepted")
            .HasFilter("\"IsDeleted\" = false AND \"Status\" = 'Accepted'");

        builder.HasOne(x => x.EmergencyRequest)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.EmergencyRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Lawyer)
            .WithMany()
            .HasForeignKey(x => x.LawyerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmergencyTimelineEventConfiguration : BaseEntityConfiguration<EmergencyTimelineEvent>
{
    public override void Configure(EntityTypeBuilder<EmergencyTimelineEvent> builder)
    {
        base.Configure(builder);
        builder.ToTable("EmergencyTimelineEvents");
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(512).IsRequired();
        builder.Property(x => x.MetadataJson).HasMaxLength(4000);
        builder.HasIndex(x => new { x.EmergencyRequestId, x.OccurredAtUtc });

        builder.HasOne(x => x.EmergencyRequest)
            .WithMany(x => x.Timeline)
            .HasForeignKey(x => x.EmergencyRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EmergencyHistoryConfiguration : BaseEntityConfiguration<EmergencyHistory>
{
    public override void Configure(EntityTypeBuilder<EmergencyHistory> builder)
    {
        base.Configure(builder);
        builder.ToTable("EmergencyHistories");
        builder.Property(x => x.FinalStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CancellationReason).HasMaxLength(512);
        builder.HasIndex(x => x.EmergencyRequestId).IsUnique().HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.ClosedAtUtc);
        builder.HasIndex(x => x.FinalStatus);
        builder.HasIndex(x => x.LawyerId);
        builder.HasIndex(x => x.CustomerId);

        builder.HasOne(x => x.EmergencyRequest)
            .WithMany()
            .HasForeignKey(x => x.EmergencyRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

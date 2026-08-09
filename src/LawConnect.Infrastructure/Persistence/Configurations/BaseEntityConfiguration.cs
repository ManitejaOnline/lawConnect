using LawConnect.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedBy).HasMaxLength(128);
        builder.Property(e => e.UpdatedBy).HasMaxLength(128);
        builder.Property(e => e.DeletedBy).HasMaxLength(128);

        builder.Property(e => e.RowVersion)
            .IsConcurrencyToken()
            .HasDefaultValue(0u);

        builder.Ignore(e => e.DomainEvents);

        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => e.CreatedAt);
    }
}

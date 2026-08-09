using LawConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LawConnect.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : BaseEntityConfiguration<Conversation>
{
    public override void Configure(EntityTypeBuilder<Conversation> builder)
    {
        base.Configure(builder);
        builder.ToTable("Conversations");
        builder.HasIndex(x => x.LastMessageAtUtc);
    }
}

public sealed class ConversationParticipantConfiguration : BaseEntityConfiguration<ConversationParticipant>
{
    public override void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        base.Configure(builder);
        builder.ToTable("ConversationParticipants");
        builder.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Participants)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MessageConfiguration : BaseEntityConfiguration<Message>
{
    public override void Configure(EntityTypeBuilder<Message> builder)
    {
        base.Configure(builder);
        builder.ToTable("Messages");
        builder.Property(x => x.Body).HasMaxLength(4000);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => new { x.ConversationId, x.SentAtUtc, x.Id });
        builder.HasIndex(x => x.SenderUserId);

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MessageAttachmentConfiguration : BaseEntityConfiguration<MessageAttachment>
{
    public override void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        base.Configure(builder);
        builder.ToTable("MessageAttachments");
        builder.Property(x => x.FileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.StorageKey).IsUnique();

        builder.HasOne(x => x.Message)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MessageReadStatusConfiguration : BaseEntityConfiguration<MessageReadStatus>
{
    public override void Configure(EntityTypeBuilder<MessageReadStatus> builder)
    {
        base.Configure(builder);
        builder.ToTable("MessageReadStatuses");
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => new { x.MessageId, x.UserId })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(x => x.Message)
            .WithMany(x => x.ReadStatuses)
            .HasForeignKey(x => x.MessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

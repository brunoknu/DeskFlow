using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class TicketAttachmentConfiguration : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.StoredFileName).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.FileHash).HasMaxLength(64).IsRequired();
        builder.Property(a => a.FileSize).IsRequired();
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => a.TicketId);
        builder.HasIndex(a => a.FileHash);
    }
}

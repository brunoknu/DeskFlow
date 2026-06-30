using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class TicketCommentConfiguration : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .HasMaxLength(TicketComment.MaxContentLength)
            .IsRequired();

        builder.Property(c => c.IsInternal).IsRequired();
        builder.Property(c => c.AuthorId).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        // Índice composto para filtrar notas internas na query sem exposição acidental.
        builder.HasIndex(c => new { c.TicketId, c.IsInternal });
        builder.HasIndex(c => c.AuthorId);
    }
}

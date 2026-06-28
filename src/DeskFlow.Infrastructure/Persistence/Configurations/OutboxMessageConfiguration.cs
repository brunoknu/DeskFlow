using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Type).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Payload).HasMaxLength(8000).IsRequired();
        builder.Property(o => o.LastError).HasMaxLength(500);
        builder.Property(o => o.OccurredAtUtc).IsRequired();

        builder.HasIndex(o => o.ProcessedAtUtc);
        builder.HasIndex(o => o.NextAttemptAtUtc);
        builder.HasIndex(o => new { o.ProcessedAtUtc, o.NextAttemptAtUtc });
    }
}

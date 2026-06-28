using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(50).IsRequired();
        builder.Property(a => a.CorrelationId).HasMaxLength(50);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.Metadata).HasMaxLength(4000);
        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.HasIndex(a => a.EntityType);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.ActorUserId);
        builder.HasIndex(a => a.OccurredAtUtc);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}

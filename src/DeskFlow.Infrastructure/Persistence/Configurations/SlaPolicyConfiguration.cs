using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Priority).IsRequired();
        builder.Property(s => s.FirstResponseMinutes).IsRequired();
        builder.Property(s => s.ResolutionMinutes).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.CreatedAtUtc).IsRequired();
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.HasIndex(s => s.Priority);
        builder.HasIndex(s => new { s.Priority, s.IsActive });
    }
}

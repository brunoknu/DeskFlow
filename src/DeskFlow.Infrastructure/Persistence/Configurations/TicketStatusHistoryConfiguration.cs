using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
{
    public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.PreviousStatus).IsRequired();
        builder.Property(h => h.NewStatus).IsRequired();
        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAtUtc).IsRequired();
        builder.Property(h => h.Reason).HasMaxLength(500);
        builder.HasIndex(h => h.TicketId);
        builder.HasIndex(h => h.ChangedAtUtc);
    }
}

using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class TicketAssignmentHistoryConfiguration : IEntityTypeConfiguration<TicketAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<TicketAssignmentHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAtUtc).IsRequired();
        builder.Property(h => h.Reason).HasMaxLength(500);
        builder.HasIndex(h => h.TicketId);
        builder.HasIndex(h => h.ChangedAtUtc);
    }
}

using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class TicketRatingConfiguration : IEntityTypeConfiguration<TicketRating>
{
    public void Configure(EntityTypeBuilder<TicketRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Score).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(1000);
        builder.Property(r => r.RequesterId).IsRequired();
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.HasIndex(r => r.TicketId).IsUnique();
    }
}

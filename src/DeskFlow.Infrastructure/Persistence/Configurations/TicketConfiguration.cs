using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DeskFlow.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Number)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Title)
            .HasMaxLength(Ticket.TitleMaxLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(Ticket.DescriptionMaxLength)
            .IsRequired();

        builder.Property(t => t.ResolutionSummary)
            .HasMaxLength(Ticket.ResolutionSummaryMaxLength);

        builder.Property(t => t.Priority).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.RequesterId).IsRequired();
        builder.Property(t => t.DepartmentId).IsRequired();
        builder.Property(t => t.CategoryId).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.UpdatedAtUtc).IsRequired();
        builder.Property(t => t.FirstResponseDueAtUtc).IsRequired();
        builder.Property(t => t.ResolutionDueAtUtc).IsRequired();
        builder.Property(t => t.ReopenCount).IsRequired().HasDefaultValue(0);

        // Token de concorrência otimista.
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Acessa as coleções pelos campos privados de backing, expondo-as via IReadOnlyList.
        builder.Navigation(t => t.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(t => t.AssignmentHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Comments)
            .WithOne()
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Attachments)
            .WithOne()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.AssignmentHistory)
            .WithOne()
            .HasForeignKey(h => h.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Rating)
            .WithOne()
            .HasForeignKey<TicketRating>(r => r.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices
        builder.HasIndex(t => t.Number).IsUnique();
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.AssignedAgentId);
        builder.HasIndex(t => t.RequesterId);
        builder.HasIndex(t => t.CategoryId);
        builder.HasIndex(t => t.CreatedAtUtc);
        builder.HasIndex(t => t.ResolutionDueAtUtc);
        builder.HasIndex(t => t.FirstResponseDueAtUtc);
        builder.HasIndex(t => new { t.Status, t.AssignedAgentId });
        builder.HasIndex(t => new { t.Status, t.Priority });
        builder.HasIndex(t => new { t.Status, t.ResolutionDueAtUtc });
    }
}

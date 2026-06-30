using DeskFlow.Application.Common;
using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Contracts;

public interface IApplicationDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<SlaPolicy> SlaPolicies { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }

    // Projection-only: exposes user data without leaking Infrastructure types into Application.
    IQueryable<UserSummary> AppUsers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

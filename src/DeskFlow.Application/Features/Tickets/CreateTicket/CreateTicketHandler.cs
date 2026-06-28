using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Identity;
using DeskFlow.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.CreateTicket;

public class CreateTicketHandler
{
    private readonly ApplicationDbContext _db;
    private readonly ITicketNumberGenerator _numberGenerator;
    private readonly IAuditLogger _audit;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _time;
    private readonly IValidator<CreateTicketCommand> _validator;

    public CreateTicketHandler(
        ApplicationDbContext db,
        ITicketNumberGenerator numberGenerator,
        IAuditLogger audit,
        UserManager<ApplicationUser> userManager,
        TimeProvider time,
        IValidator<CreateTicketCommand> validator)
    {
        _db = db;
        _numberGenerator = numberGenerator;
        _audit = audit;
        _userManager = userManager;
        _time = time;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(CreateTicketCommand cmd, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            return Result.Failure<Guid>(validation.Errors[0].ErrorMessage);

        var user = await _userManager.FindByIdAsync(cmd.RequesterId.ToString());
        if (user is null || !user.IsActive)
            return Result.Failure<Guid>("User is not active or does not exist.");

        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == cmd.DepartmentId && d.IsActive, ct);
        if (department is null)
            return Result.Failure<Guid>("Department not found.");

        var category = await _db.TicketCategories
            .FirstOrDefaultAsync(c => c.Id == cmd.CategoryId && c.IsActive, ct);
        if (category is null)
            return Result.Failure<Guid>("Category not found.");

        var sla = await _db.SlaPolicies
            .Where(s => s.Priority == cmd.Priority && s.IsActive)
            .FirstOrDefaultAsync(ct);
        if (sla is null)
            return Result.Failure<Guid>($"No active SLA policy found for priority '{cmd.Priority}'.");

        var now = _time.GetUtcNow();
        var number = await _numberGenerator.GenerateAsync(ct);

        var ticket = Ticket.Create(
            number,
            cmd.Title,
            cmd.Description,
            cmd.RequesterId,
            cmd.DepartmentId,
            cmd.CategoryId,
            cmd.Priority,
            sla.CalculateFirstResponseDue(now),
            sla.CalculateResolutionDue(now),
            now);

        _db.Tickets.Add(ticket);

        // Outbox notification: ticket created
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            TicketId = ticket.Id,
            Number = ticket.Number,
            Title = ticket.Title,
            RequesterId = cmd.RequesterId
        });
        _db.OutboxMessages.Add(OutboxMessage.Create("TicketCreated", payload, now));

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("TicketCreated", "Ticket", ticket.Id.ToString(), cmd.RequesterId, ct: ct);

        return Result.Success(ticket.Id);
    }
}

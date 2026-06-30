using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.CreateTicket;

public class CreateTicketHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ITicketNumberGenerator _numberGenerator;
    private readonly IAuditLogger _audit;
    private readonly IUserService _users;
    private readonly TimeProvider _time;
    private readonly IValidator<CreateTicketCommand> _validator;

    public CreateTicketHandler(
        IApplicationDbContext db,
        ITicketNumberGenerator numberGenerator,
        IAuditLogger audit,
        IUserService users,
        TimeProvider time,
        IValidator<CreateTicketCommand> validator)
    {
        _db = db;
        _numberGenerator = numberGenerator;
        _audit = audit;
        _users = users;
        _time = time;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(CreateTicketCommand cmd, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(cmd, ct);
        if (!validation.IsValid)
            return Result.Failure<Guid>(validation.Errors[0].ErrorMessage);

        if (!await _users.IsActiveAsync(cmd.RequesterId, ct))
            return Result.Failure<Guid>("Usuário inativo ou não encontrado.");

        var department = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == cmd.DepartmentId && d.IsActive, ct);
        if (department is null)
            return Result.Failure<Guid>("Departamento não encontrado.");

        var category = await _db.TicketCategories
            .FirstOrDefaultAsync(c => c.Id == cmd.CategoryId && c.IsActive, ct);
        if (category is null)
            return Result.Failure<Guid>("Categoria não encontrada.");

        var sla = await _db.SlaPolicies
            .Where(s => s.Priority == cmd.Priority && s.IsActive)
            .FirstOrDefaultAsync(ct);
        if (sla is null)
            return Result.Failure<Guid>($"Nenhuma política de SLA ativa encontrada para a prioridade '{cmd.Priority}'.");

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

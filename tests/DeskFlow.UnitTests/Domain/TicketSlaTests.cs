using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using FluentAssertions;

namespace DeskFlow.UnitTests.Domain;

public class TicketSlaTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

    private static Ticket CreateTicketWithSla(DateTimeOffset firstResponseDue, DateTimeOffset resolutionDue)
        => Ticket.Create(
            "HD-2026-000006",
            "Título",
            "Descrição.",
            UserId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            TicketPriority.High,
            firstResponseDue,
            resolutionDue,
            Now);

    [Fact]
    public void Ticket_is_not_breached_before_deadline()
    {
        var ticket = CreateTicketWithSla(Now.AddHours(1), Now.AddHours(8));
        ticket.IsFirstResponseBreached(Now.AddMinutes(30)).Should().BeFalse();
        ticket.IsResolutionBreached(Now.AddHours(4)).Should().BeFalse();
    }

    [Fact]
    public void First_response_is_breached_after_deadline()
    {
        var ticket = CreateTicketWithSla(Now.AddHours(1), Now.AddHours(8));
        ticket.IsFirstResponseBreached(Now.AddHours(2)).Should().BeTrue();
    }

    [Fact]
    public void First_response_not_breached_when_response_recorded()
    {
        var ticket = CreateTicketWithSla(Now.AddHours(1), Now.AddHours(8));
        ticket.RecordFirstResponse(Now.AddMinutes(45));
        ticket.IsFirstResponseBreached(Now.AddHours(3)).Should().BeFalse();
    }

    [Fact]
    public void Resolution_is_breached_after_deadline()
    {
        var ticket = CreateTicketWithSla(Now.AddHours(1), Now.AddHours(8));
        ticket.IsResolutionBreached(Now.AddHours(10)).Should().BeTrue();
    }

    [Fact]
    public void Resolved_ticket_is_never_resolution_breached()
    {
        var ticket = CreateTicketWithSla(Now.AddHours(1), Now.AddHours(8));
        ticket.Transition(TicketStatus.Triaged, UserId, Now);
        ticket.Transition(TicketStatus.InProgress, UserId, Now);
        ticket.Resolve("Resolvido.", UserId, Now);

        ticket.IsResolutionBreached(Now.AddHours(20)).Should().BeFalse();
    }
}

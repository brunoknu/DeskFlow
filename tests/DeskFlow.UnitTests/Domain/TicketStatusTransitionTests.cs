using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Domain.Exceptions;
using FluentAssertions;

namespace DeskFlow.UnitTests.Domain;

public class TicketStatusTransitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

    private static Ticket CreateTicket(TicketPriority priority = TicketPriority.Medium)
        => Ticket.Create(
            "HD-2026-000001",
            "Título do chamado",
            "Descrição do problema.",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            priority,
            Now.AddHours(4),
            Now.AddHours(24),
            Now);

    [Fact]
    public void New_ticket_starts_with_New_status()
    {
        var ticket = CreateTicket();
        ticket.Status.Should().Be(TicketStatus.New);
    }

    [Fact]
    public void Can_transition_from_New_to_Triaged()
    {
        var ticket = CreateTicket();
        ticket.Transition(TicketStatus.Triaged, UserId, Now);
        ticket.Status.Should().Be(TicketStatus.Triaged);
    }

    [Fact]
    public void Can_transition_from_Triaged_to_InProgress()
    {
        var ticket = CreateTicket();
        ticket.Transition(TicketStatus.Triaged, UserId, Now);
        ticket.Transition(TicketStatus.InProgress, UserId, Now);
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void Cannot_skip_directly_from_New_to_InProgress()
    {
        var ticket = CreateTicket();
        var act = () => ticket.Transition(TicketStatus.InProgress, UserId, Now);
        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void Cannot_transition_from_Cancelled()
    {
        var ticket = CreateTicket();
        ticket.Cancel(UserId, Now);
        var act = () => ticket.Transition(TicketStatus.New, UserId, Now);
        act.Should().Throw<InvalidStatusTransitionException>();
    }

    [Fact]
    public void Transition_records_status_history()
    {
        var ticket = CreateTicket();
        ticket.Transition(TicketStatus.Triaged, UserId, Now, "Triaged by agent");
        ticket.StatusHistory.Should().ContainSingle();
        ticket.StatusHistory[0].PreviousStatus.Should().Be(TicketStatus.New);
        ticket.StatusHistory[0].NewStatus.Should().Be(TicketStatus.Triaged);
    }

    [Fact]
    public void Resolving_sets_ResolvedAtUtc()
    {
        var ticket = CreateTicket();
        ticket.Transition(TicketStatus.Triaged, UserId, Now);
        ticket.Transition(TicketStatus.InProgress, UserId, Now);
        ticket.Resolve("Problema resolvido reinstalando o driver.", UserId, Now);
        ticket.ResolvedAtUtc.Should().Be(Now);
        ticket.Status.Should().Be(TicketStatus.Resolved);
    }

    [Fact]
    public void Resolving_without_summary_throws()
    {
        var ticket = CreateTicket();
        ticket.Transition(TicketStatus.Triaged, UserId, Now);
        ticket.Transition(TicketStatus.InProgress, UserId, Now);
        var act = () => ticket.Resolve("", UserId, Now);
        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(TicketStatus.New, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.Triaged, TicketStatus.Cancelled)]
    public void Requester_can_cancel_before_InProgress(TicketStatus from, TicketStatus to)
    {
        var ticket = CreateTicket();
        if (from == TicketStatus.Triaged)
            ticket.Transition(TicketStatus.Triaged, UserId, Now);

        var act = () => ticket.Transition(to, UserId, Now);
        act.Should().NotThrow();
        ticket.Status.Should().Be(TicketStatus.Cancelled);
    }
}

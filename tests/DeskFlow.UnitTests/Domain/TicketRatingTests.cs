using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Domain.Exceptions;
using FluentAssertions;

namespace DeskFlow.UnitTests.Domain;

public class TicketRatingTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid AnotherUser = Guid.NewGuid();

    private static Ticket CreateResolvedTicket()
    {
        var ticket = Ticket.Create(
            "HD-2026-000004",
            "Título",
            "Descrição.",
            RequesterId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            TicketPriority.High,
            Now.AddHours(1),
            Now.AddHours(8),
            Now);

        ticket.Transition(TicketStatus.Triaged, RequesterId, Now);
        ticket.Transition(TicketStatus.InProgress, RequesterId, Now);
        ticket.Resolve("Problema corrigido.", RequesterId, Now);
        return ticket;
    }

    [Fact]
    public void Requester_can_rate_resolved_ticket()
    {
        var ticket = CreateResolvedTicket();
        ticket.AddRating(RequesterId, 5, "Ótimo atendimento!", Now);
        ticket.Rating.Should().NotBeNull();
        ticket.Rating!.Score.Should().Be(5);
    }

    [Fact]
    public void Cannot_rate_twice()
    {
        var ticket = CreateResolvedTicket();
        ticket.AddRating(RequesterId, 4, null, Now);
        var act = () => ticket.AddRating(RequesterId, 5, null, Now);
        act.Should().Throw<DomainException>().WithMessage("*already been rated*");
    }

    [Fact]
    public void Cannot_rate_if_not_requester()
    {
        var ticket = CreateResolvedTicket();
        var act = () => ticket.AddRating(AnotherUser, 5, null, Now);
        act.Should().Throw<DomainException>().WithMessage("*requester*");
    }

    [Fact]
    public void Cannot_rate_ticket_that_is_not_resolved()
    {
        var ticket = Ticket.Create(
            "HD-2026-000005",
            "Título",
            "Descrição.",
            RequesterId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            TicketPriority.Low,
            Now.AddHours(8),
            Now.AddHours(72),
            Now);

        var act = () => ticket.AddRating(RequesterId, 3, null, Now);
        act.Should().Throw<DomainException>().WithMessage("*resolution*");
    }
}

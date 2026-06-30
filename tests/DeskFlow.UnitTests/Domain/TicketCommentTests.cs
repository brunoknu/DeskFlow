using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using DeskFlow.Domain.Exceptions;
using FluentAssertions;

namespace DeskFlow.UnitTests.Domain;

public class TicketCommentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AgentId = Guid.NewGuid();
    private static readonly Guid RequesterId = Guid.NewGuid();

    private static Ticket CreateOpenTicket()
        => Ticket.Create(
            "HD-2026-000003",
            "Título",
            "Descrição.",
            RequesterId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            TicketPriority.Medium,
            Now.AddHours(4),
            Now.AddHours(24),
            Now);

    [Fact]
    public void Public_comment_is_visible()
    {
        var ticket = CreateOpenTicket();
        ticket.AddPublicComment(RequesterId, "Preciso de ajuda com o sistema.", Now);
        ticket.Comments.Should().ContainSingle(c => !c.IsInternal);
    }

    [Fact]
    public void Internal_note_is_marked_as_internal()
    {
        var ticket = CreateOpenTicket();
        ticket.AddInternalNote(AgentId, "Verificar com o fornecedor.", Now);
        ticket.Comments.Should().ContainSingle(c => c.IsInternal);
    }

    [Fact]
    public void Cannot_add_comment_to_closed_ticket()
    {
        var ticket = CreateOpenTicket();
        ticket.Transition(TicketStatus.Triaged, AgentId, Now);
        ticket.Transition(TicketStatus.InProgress, AgentId, Now);
        ticket.Resolve("Resolvido.", AgentId, Now);
        ticket.Transition(TicketStatus.Closed, AgentId, Now);

        var act = () => ticket.AddPublicComment(RequesterId, "Tenho uma dúvida.", Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cannot_add_comment_to_cancelled_ticket()
    {
        var ticket = CreateOpenTicket();
        ticket.Cancel(RequesterId, Now);

        var act = () => ticket.AddPublicComment(RequesterId, "Comentário.", Now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void First_response_is_recorded_on_first_public_comment()
    {
        var ticket = CreateOpenTicket();
        ticket.RecordFirstResponse(Now.AddMinutes(30));
        ticket.FirstResponseAtUtc.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public void First_response_is_not_overwritten()
    {
        var ticket = CreateOpenTicket();
        ticket.RecordFirstResponse(Now.AddMinutes(30));
        ticket.RecordFirstResponse(Now.AddHours(2));
        ticket.FirstResponseAtUtc.Should().Be(Now.AddMinutes(30));
    }
}

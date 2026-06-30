using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Enums;
using FluentAssertions;

namespace DeskFlow.UnitTests.Domain;

public class TicketValidationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Creating_ticket_with_empty_title_throws()
    {
        var act = () => Ticket.Create("HD-001", "", "Descrição válida.", UserId,
            Guid.NewGuid(), Guid.NewGuid(), TicketPriority.Low, Now.AddHours(8), Now.AddHours(72), Now);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Creating_ticket_with_empty_description_throws()
    {
        var act = () => Ticket.Create("HD-001", "Título válido", "", UserId,
            Guid.NewGuid(), Guid.NewGuid(), TicketPriority.Low, Now.AddHours(8), Now.AddHours(72), Now);

        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Creating_ticket_with_title_exceeding_max_length_throws()
    {
        var longTitle = new string('A', Ticket.TitleMaxLength + 1);
        var act = () => Ticket.Create("HD-001", longTitle, "Descrição.", UserId,
            Guid.NewGuid(), Guid.NewGuid(), TicketPriority.Low, Now.AddHours(8), Now.AddHours(72), Now);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Title_is_trimmed_on_creation()
    {
        var ticket = Ticket.Create("HD-001", "  Título com espaços  ", "Descrição.", UserId,
            Guid.NewGuid(), Guid.NewGuid(), TicketPriority.Low, Now.AddHours(8), Now.AddHours(72), Now);

        ticket.Title.Should().Be("Título com espaços");
    }
}

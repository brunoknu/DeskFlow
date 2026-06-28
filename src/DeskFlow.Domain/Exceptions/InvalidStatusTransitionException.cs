using DeskFlow.Domain.Enums;

namespace DeskFlow.Domain.Exceptions;

public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(TicketStatus from, TicketStatus to)
        : base($"Cannot transition ticket from '{from}' to '{to}'.") { }
}

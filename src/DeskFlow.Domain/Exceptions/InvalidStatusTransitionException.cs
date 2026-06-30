using DeskFlow.Domain.Enums;

namespace DeskFlow.Domain.Exceptions;

public class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(TicketStatus from, TicketStatus to)
        : base($"Transição de status inválida: de '{from}' para '{to}'.") { }
}
